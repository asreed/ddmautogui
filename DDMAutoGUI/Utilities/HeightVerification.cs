using DDMAutoGUI.Services;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.Statistics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace DDMAutoGUI.Utilities
{

    public class HeightVerificationResult
    {
        public bool passed { get; set; } = false;
        public double normMaxHeight { get; set; }
        public double normMinHeight { get; set; }
        public double refA { get; set; }
        public double refPhi { get; set; }
        public double refRSquared { get; set; }
        public List<ResultsHeightMeasurement> refData { get; set; }
        public List<ResultsHeightMeasurement> ringData { get; set; }
        public List<ResultsHeightMeasurement> magConcData { get; set; }
        public List<ResultsHeightMeasurement> normRingData { get; set; }
        public List<ResultsHeightMeasurement> normMagConcData { get; set; }
        public string message { get; set; } = "";

    }


    public static class HeightVerification
    {
        static HeightVerification() { }




        public static HeightVerificationResult VerifyHeightData(
            List<ResultsHeightMeasurement> refData,
            List<ResultsHeightMeasurement> ringData, 
            List<ResultsHeightMeasurement> magConcData, 
            CSMotor motor,
            CellSettings settings)
        {

            // Remove invalid readings from data
            const float badSensorRead = -100000000f;
            List<ResultsHeightMeasurement> refDataTrimmed = refData
                .Where(m => m.z.HasValue && m.z.Value > badSensorRead * 0.9f)
                .ToList();

            List<ResultsHeightMeasurement> ringDataTrimmed = ringData
                .Where(m => m.z.HasValue && m.z.Value > badSensorRead * 0.9f)
                .ToList();

            List<ResultsHeightMeasurement> magConcDataTrimmed = magConcData
                .Where(m => m.z.HasValue && m.z.Value > badSensorRead * 0.9f)
                .ToList();


            // Fit sine to ring data
            double A, phi, zOffset, rSquared;
            FitSinToData(refDataTrimmed, out A, out phi, out zOffset, out rSquared);

            // Normalize ring and mag conc data based on fit
            List<ResultsHeightMeasurement> normRingData = NormalizeData(A, phi, zOffset, ringDataTrimmed);
            List<ResultsHeightMeasurement> normMagConcData = NormalizeData(A, phi, zOffset, magConcDataTrimmed);

            // Get min/max of normalized data
            float maxRingHeight = normRingData.Max(m => m.z) ?? float.NaN;
            float minRingHeight = normRingData.Min(m => m.z) ?? float.NaN;
            float maxMCHeight = normMagConcData.Max(m => m.z) ?? float.NaN;
            float minMCHeight = normMagConcData.Min(m => m.z) ?? float.NaN;

            // Settings data in mm, convert to um for comparison with normalized data
            float maxAcceptableSinAmplitude = motor.sin_fit_max_amplitude.Value * 1000f;
            float maxAcceptableRingHeight = motor.ring_height_max.Value * 1000f;
            float minAcceptableRingHeight = motor.ring_height_min.Value * 1000f;
            float maxAcceptableMCHeight = motor.mag_height_max.Value * 1000f;
            float minAcceptableMCHeight = motor.mag_height_min.Value * 1000f;


            HeightVerificationResult result = new HeightVerificationResult();
            result.refData = refDataTrimmed;
            result.refA = A;
            result.refPhi = phi;
            result.refRSquared = rSquared;
            result.ringData = ringData; // save out raw data, including any bad reads
            result.magConcData = magConcData;
            result.normRingData = normRingData;
            result.normMagConcData = normMagConcData;

            List<string> failures = new List<string>();

            // Guard: usable input and a usable fit
            if (refDataTrimmed.Count == 0)
            {
                failures.Add("No valid reference readings after removing bad sensor reads.\n");
            }
            else if (double.IsNaN(A) || double.IsNaN(rSquared))
            {
                failures.Add("Sine fit to reference data did not converge.\n");
            }

            if (failures.Count == 0)
            {
                CheckRange("Tool height variation", Math.Abs(A), 0, maxAcceptableSinAmplitude, failures);
                CheckRange("Ring height", maxRingHeight, minAcceptableRingHeight, maxAcceptableRingHeight, failures);
                CheckRange("Ring height", minRingHeight, minAcceptableRingHeight, maxAcceptableRingHeight, failures);
                CheckRange("Mag/conc height", maxMCHeight, minAcceptableMCHeight, maxAcceptableMCHeight, failures);
                CheckRange("Mag/conc height", minMCHeight, minAcceptableMCHeight, maxAcceptableMCHeight, failures);
            }

            result.passed = failures.Count == 0;
            result.message = result.passed
                ? $"Passed. Tool variation {Math.Abs(A):F1} (R^2 {rSquared:F3}), ring {minRingHeight:F1}-{maxRingHeight:F1}, mag/conc {minMCHeight:F1}-{maxMCHeight:F1}."
                : string.Join(Environment.NewLine, failures);

            return result;
        }

        private static void CheckRange(string label, double value, double min, double max, List<string> failures)
        {
            if (double.IsNaN(value))
            {
                failures.Add($"{label} could not be determined (no valid readings).\n");
            }
            else if (value > max)
            {
                failures.Add($"{label} {value:F1} above acceptable limit {max:F1}.\n");
            }
            else if (value < min)
            {
                failures.Add($"{label} {value:F1} below acceptable limit {min:F1}.\n");
            }
        }







        public static List<ResultsHeightMeasurement> NormalizeData(double A, double phi, double zOffset, List<ResultsHeightMeasurement> rawData)
        {
            List<ResultsHeightMeasurement> normData = new List<ResultsHeightMeasurement>();
            for (int i = 0; i < rawData.Count; i++)
            {
                double t = rawData[i].t.Value;
                double z = rawData[i].z.Value;
                double zFit = A * Math.Sin(t * (Math.PI / 180.0) + phi) + zOffset;
                double zNorm = z - zFit;
                normData.Add(new ResultsHeightMeasurement { t = (float)t, z = (float)zNorm });
            }
            return normData;
        }

        public static void FitSinToData(List<ResultsHeightMeasurement> rawDataList, out double A, out double phi, out double zOffset, out double rSquared)
        {
            // https://math.stackexchange.com/questions/902166/fit-sine-wave-to-data
            // https://math.stackexchange.com/questions/3926007/least-squares-regression-of-sine-wave

            // Assuming period of 2pi to fit the relation:
            // y(t) = A * sin(t + phi)

            // y(t) = A * sin(t) * cos(phi) + A * cos(t) * sin(phi)
            // w = sin(t)
            // z = cos(t)
            // A1 = A * cos(phi)
            // A2 = A * sin(phi)
            // Y = [w, z] * [A1; A2]
            // Y = X * B
            // ... 
            // B = inv(X' * X) * X' * Y
            // ... 
            // A^2 = A1^2 + A2^2
            // phi = atan(A2 / A1)

            // Convert list to array for Mathnet
            double[,] rawDataArray = new double[rawDataList.Count, 2];
            for (int i = 0; i < rawDataList.Count; i++)
            {
                rawDataArray[i, 0] = (double)rawDataList[i].t; // angle
                rawDataArray[i, 1] = (double)rawDataList[i].z; // height
            }

            var M = Matrix<double>.Build;
            var V = Vector<double>.Build;

            var data = M.DenseOfArray(rawDataArray);
            var ones = V.Dense(data.RowCount, 1);

            // Vector of angle in radians
            var angRad = data.Column(0) * (Math.PI / 180.0);

            // Vector of heights, shifted for zero mean
            var height = data.Column(1);
            var offset = data.Column(1).Mean();
            var heightShifted = height - offset;

            var dataShifted = M.DenseOfColumnVectors(data.Column(0), heightShifted);

            var w = angRad.PointwiseSin();
            var z = angRad.PointwiseCos();
            var X = M.DenseOfColumnVectors(w, z);

            var B = (X.Transpose() * X).Inverse() * X.Transpose() * heightShifted;

            A = Math.Sqrt(B[0] * B[0] + B[1] * B[1]);
            phi = Math.Atan(B[1] / B[0]);

            // Two possible solutions for A
            // Verify with R^2

            double[,] fitData = GenerateSinCurve(rawDataArray, A, phi);
            rSquared = GetRSquared(dataShifted.ToArray(), fitData);
            zOffset = offset;

            if (Math.Abs(rSquared) > 1.0)
            {
                // try other A
                A *= -1;
                fitData = GenerateSinCurve(rawDataArray, A, phi);
                rSquared = GetRSquared(dataShifted.ToArray(), fitData);
                if (rSquared < 0 || rSquared > 1)
                {
                    // something else is wrong
                    Debug.Print("Sine fit failed. R^2 out of range.");
                    A = double.NaN;
                    phi = double.NaN;
                    zOffset = double.NaN;
                    rSquared = double.NaN;
                    return;
                }
            }
        }

        private static double[,] GenerateSinCurve(double[,] rawData, double A, double phi)
        {
            // y(t) = A * sin(x(t) + phi)

            double[,] fitData = new double[rawData.GetLength(0), 2];
            for (int i = 0; i < rawData.GetLength(0); i++)
            {
                double x = rawData[i, 0];
                double yFit = A * Math.Sin(x * (Math.PI / 180.0) + phi);
                fitData[i, 0] = x;
                fitData[i, 1] = yFit;
            }
            return fitData;
        }

        private static double GetRSquared(double[,] rawData, double[,] fitData)
        {
            var M = Matrix<double>.Build;
            var data = M.DenseOfArray(rawData);
            var fit = M.DenseOfArray(fitData);
            var ssRes = (data.Column(1) - fit.Column(1)).PointwisePower(2).Sum();
            var ssTot = (data.Column(1) - data.Column(1).Mean()).PointwisePower(2).Sum();
            var rSquared = 1 - (ssRes / ssTot);
            return rSquared;
        }

        public static void PrintHeightData(List<ResultsHeightMeasurement> data)
        {
            Debug.Print("t\tz");
            foreach (var item in data)
            {
                Debug.Print($"{item.t}\t{item.z}");
            }

        }

    }
}
