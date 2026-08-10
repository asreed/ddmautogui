using DDMAutoGUI.Services;
using MathNet.Numerics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DDMAutoGUI.Utilities
{
    public class PolarityVerificationResult
    {
        public bool passed { get; set; } = false;
        public int numPeaks { get; set; }
        public int expectedMagnets { get; set; }
        public int numLongWavelengths { get; set; }
        public int numShortWavelengths { get; set; }
        public double expectedWavelength { get; set; }
        public double[] time { get; set; }
        public double[] rawSignal { get; set; }
        public double[] filteredSignal { get; set; }
        public int[] extremaIndices { get; set; }
        public double[] wavelengths { get; set; }
        public int errorCode { get; set; }
        public string message { get; set; } = "";
    }

    public static class PolarityVerification
    {
        static PolarityVerification() { }

        private const int WindowSize = 10;
        private const double CutoffFreq = 100.0;
        private const int FilterOrder = 4;

        public static PolarityVerificationResult VerifyPolarityData(
            double[] time,
            double[] signal,
            double sampleRate,
            CSMotor motor)
        {
            PolarityVerificationResult result = new PolarityVerificationResult();
            result.time = time;
            result.rawSignal = signal;

            List<string> failures = new List<string>();

            int expectedMagnets = motor.pol_expected_magnets ?? 0;
            double expectedWavelength = motor.pol_expected_wavelength ?? 0;
            result.expectedMagnets = expectedMagnets;
            result.expectedWavelength = expectedWavelength;

            // Guard: usable input and a usable motor definition
            if (signal == null || signal.Length == 0)
            {
                failures.Add("No Hall data to evaluate.\n");
            }
            else if (time == null || time.Length != signal.Length)
            {
                failures.Add("Hall time and signal vectors do not match.\n");
            }
            else if (expectedMagnets <= 0)
            {
                failures.Add("Motor settings do not define an expected magnet count.\n");
            }
            else if (expectedWavelength <= 0)
            {
                failures.Add("Motor settings do not define an expected wavelength.\n");
            }

            if (failures.Count == 0)
            {
                double[] filtered = ZeroPhaseLowPass(signal, sampleRate, CutoffFreq);
                int[] extrema = FindExtrema(filtered, WindowSize);
                double[] wavelengths = GetWavelengths(time, extrema);

                result.filteredSignal = filtered;
                result.extremaIndices = extrema;
                result.wavelengths = wavelengths;
                result.numPeaks = extrema.Length;

                CheckPeakCount(result.numPeaks, expectedMagnets, failures);

                if (failures.Count == 0)
                {
                    // Counted only after the peak-count gate, matching the MATLAB
                    // ordering so results files stay comparable.
                    result.numLongWavelengths = wavelengths.Count(w => w > 1.8 * expectedWavelength);
                    result.numShortWavelengths = wavelengths.Count(w => w < 0.65 * expectedWavelength);

                    // Exactly one long gap is the expected polarity signature:
                    // the single reversed pole pair. Anything else is a fault.
                    if (result.numLongWavelengths != 1 || result.numShortWavelengths != 0)
                    {
                        failures.Add(
                            $"Polarity error. Expected exactly 1 long wavelength and 0 short; " +
                            $"found {result.numLongWavelengths} long, {result.numShortWavelengths} short.\n");
                    }
                }
            }

            result.passed = failures.Count == 0;
            result.message = result.passed
                ? $"Passed. {result.numPeaks} peaks detected (expected {expectedMagnets}), " +
                  $"{result.numLongWavelengths} long / {result.numShortWavelengths} short wavelengths."
                : string.Join(Environment.NewLine, failures);

            return result;
        }

        private static void CheckPeakCount(int numPeaks, int expectedMagnets, List<string> failures)
        {
            double max = 1.25 * expectedMagnets;
            double min = 0.75 * expectedMagnets;

            if (numPeaks >= max || numPeaks <= min)
            {
                failures.Add(
                    $"Incorrect number of peaks: {numPeaks} detected, " +
                    $"expected between {min:F0} and {max:F0}.\n");
            }
        }

        /// <summary>
        /// Locates points where the signal slope changes sign consistently over
        /// a window on both sides. Window-based confirmation rejects the noise
        /// spikes that a simple sign-change test would count as peaks.
        /// </summary>
        public static int[] FindExtrema(double[] filteredSignal, int windowSize)
        {
            double[] dy = new double[filteredSignal.Length - 1];
            for (int i = 0; i < dy.Length; i++)
            {
                dy[i] = filteredSignal[i + 1] - filteredSignal[i];
            }

            List<int> extrema = new List<int>();
            for (int i = windowSize; i < dy.Length - windowSize; i++)
            {
                bool peak = AllSameSign(dy, i - windowSize, windowSize, true)
                         && AllSameSign(dy, i, windowSize, false);
                bool valley = AllSameSign(dy, i - windowSize, windowSize, false)
                           && AllSameSign(dy, i, windowSize, true);

                if (peak || valley)
                {
                    extrema.Add(i);
                }
            }
            return extrema.ToArray();
        }

        public static double[] GetWavelengths(double[] time, int[] extremaIndices)
        {
            double[] wavelengths = new double[Math.Max(0, extremaIndices.Length - 1)];
            for (int i = 0; i < wavelengths.Length; i++)
            {
                wavelengths[i] = time[extremaIndices[i + 1]] - time[extremaIndices[i]];
            }
            return wavelengths;
        }

        private static bool AllSameSign(double[] dy, int start, int count, bool positive)
        {
            for (int i = start; i < start + count; i++)
            {
                if (positive ? dy[i] <= 0 : dy[i] >= 0) return false;
            }
            return true;
        }

        /// <summary>
        /// Zero-phase low-pass filter matching MATLAB's
        /// [b,a] = butter(4, cutoff/(rate/2), 'low'); filtfilt(b, a, signal).
        /// The forward-backward pass cancels phase shift and doubles the
        /// effective order, so a 4th-order design attenuates like 8th-order.
        /// </summary>
        public static double[] ZeroPhaseLowPass(double[] signal, double sampleRate, double cutoff)
        {
            if (signal == null || signal.Length == 0) return signal;

            double nyquist = sampleRate / 2.0;
            double wn = cutoff / nyquist;

            // Nothing to do if the cutoff is at or above Nyquist.
            if (wn >= 1.0) return (double[])signal.Clone();

            Biquad[] sections = DesignButterworthLowPass(FilterOrder, wn);

            // MATLAB pads by 3*(len(a)-1); for a 4th-order design len(a) is 5.
            int padLen = 6 * (FilterOrder + 1);

            // Not enough data to pad - filter without it rather than fail.
            if (signal.Length <= padLen)
            {
                return FilterForwardBackward(signal, sections);
            }

            double[] padded = ReflectPad(signal, padLen);
            double[] filtered = FilterForwardBackward(padded, sections);

            double[] trimmed = new double[signal.Length];
            Array.Copy(filtered, padLen, trimmed, 0, signal.Length);
            return trimmed;
        }

        /// <summary>
        /// Designs a digital Butterworth low-pass as a cascade of biquads using
        /// the bilinear transform with frequency pre-warping.
        /// </summary>
        /// <param name="order">Filter order; must be even.</param>
        /// <param name="wn">Cutoff normalized to Nyquist, in (0, 1).</param>
        private static Biquad[] DesignButterworthLowPass(int order, double wn)
        {
            int nSections = order / 2;
            Biquad[] sections = new Biquad[nSections];

            // Pre-warp so the digital cutoff lands on the requested frequency.
            double k = 1.0 / Math.Tan(Math.PI * wn / 2.0);
            double kk = k * k;

            for (int i = 0; i < nSections; i++)
            {
                // Analog Butterworth poles lie on the unit semicircle. Each
                // conjugate pair gives one section: s^2 + c*s + 1.
                double theta = Math.PI * (2.0 * i + order + 1.0) / (2.0 * order);
                double c = -2.0 * Math.Cos(theta);

                double a0 = kk + c * k + 1.0;

                sections[i] = new Biquad
                {
                    b0 = 1.0 / a0,
                    b1 = 2.0 / a0,
                    b2 = 1.0 / a0,
                    a1 = (2.0 - 2.0 * kk) / a0,
                    a2 = (kk - c * k + 1.0) / a0
                };
            }

            return sections;
        }

        /// <summary>
        /// Extends both ends by odd reflection about the endpoint values, as
        /// filtfilt does. This suppresses the startup transient that would
        /// otherwise distort the first and last samples - important here
        /// because a spurious edge extremum shifts the peak count.
        /// </summary>
        private static double[] ReflectPad(double[] signal, int padLen)
        {
            int n = signal.Length;
            double[] padded = new double[n + 2 * padLen];

            for (int i = 0; i < padLen; i++)
            {
                padded[i] = 2.0 * signal[0] - signal[padLen - i];
                padded[padLen + n + i] = 2.0 * signal[n - 1] - signal[n - 2 - i];
            }

            Array.Copy(signal, 0, padded, padLen, n);
            return padded;
        }

        /// <summary>
        /// Runs the cascade forward, then backward over the reversed result.
        /// The two passes have equal and opposite phase, cancelling it exactly.
        /// </summary>
        private static double[] FilterForwardBackward(double[] signal, Biquad[] sections)
        {
            double[] forward = ApplyCascade(signal, sections);
            Array.Reverse(forward);

            double[] backward = ApplyCascade(forward, sections);
            Array.Reverse(backward);

            return backward;
        }

        private static double[] ApplyCascade(double[] signal, Biquad[] sections)
        {
            double[] output = (double[])signal.Clone();
            foreach (Biquad section in sections)
            {
                output = section.Apply(output);
            }
            return output;
        }

        /// <summary>
        /// Second-order section in direct form II transposed, which is
        /// numerically well behaved for the narrow cutoffs used here.
        /// </summary>
        private struct Biquad
        {
            public double b0, b1, b2, a1, a2;

            public double[] Apply(double[] x)
            {
                double[] y = new double[x.Length];
                double z1 = 0.0, z2 = 0.0;

                for (int i = 0; i < x.Length; i++)
                {
                    double outSample = b0 * x[i] + z1;
                    z1 = b1 * x[i] - a1 * outSample + z2;
                    z2 = b2 * x[i] - a2 * outSample;
                    y[i] = outSample;
                }

                return y;
            }
        }
    }
}