using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace DDMAutoGUI.utilities
{
    public class HeightCalibration
    {
        public HeightCalibration() { }

        public static void MathNetTest()
        {

            // https://math.stackexchange.com/questions/902166/fit-sine-wave-to-data
            // https://math.stackexchange.com/questions/3926007/least-squares-regression-of-sine-wave

            // assuming period of 2pi to fit the relation:
            // y(t) = A * sin(x(t) + phi)

            // y(t) = A * sin(x(t)) * cos(phi) + A * cos(x(t)) * sin(phi)
            // w = sin(x(t))
            // z = cos(x(t))
            // A1 = A * cos(phi)
            // A2 = A * sin(phi)
            // Y = [w, z] * [A1; A2]
            // Y = X * B
            // B = (X^T * X)^-1 * X^T * Y
            // ... ?
            // A^2 = A1^2 + A2^2
            // phi = atan(A2 / A1)

            var M = Matrix<double>.Build;
            var V = Vector<double>.Build;

            double[,] rawData =
            {
                {0, -41.4},
                {4.51, -57},
                {9.02, -47.1},
                {13.53, -61.2},
                {18.01, -54},
                {22.51, -45},
                {27.01, -38.4},
                {31.51, -54.6},
                {36.02, -66.6},
                {40.52, -45.3},
                {45.02, -50.1},
                {49.52, -55.8},
                {54.02, -48},
                {58.51, -51.9},
                {63.01, -50.7},
                {67.52, -63.6},
                {72.02, -55.2},
                {76.51, -48.3},
                {81.01, -49.8},
                {85.51, -47.1},
                {90.03, -54.6},
                {94.51, -49.2},
                {99.01, -39},
                {103.51, -39},
                {108.01, -51.3},
                {112.51, -60.9},
                {117.01, -48},
                {121.51, -45.9},
                {126.02, -51.9},
                {130.51, -40.5},
                {135.01, -42},
                {139.5, -48.9},
                {144.01, -41.7},
                {148.52, -25.2},
                {153.01, -48.6},
                {157.51, -51.6},
                {162.01, -45},
                {166.51, -30.3},
                {171, -24.3},
                {175.5, -31.2},
                {180.02, -15},
                {184.51, -20.4},
                {189.01, -15.6},
                {193.5, -31.2},
                {198.01, -30.3},
                {202.51, -13.5},
                {207.01, -22.8},
                {211.52, -17.4},
                {216.02, -17.4},
                {220.52, -6.6},
                {225.01, -24.6},
                {229.51, 2.7},
                {234.02, -1.8},
                {238.51, -5.7},
                {243.01, 14.4},
                {247.51, -15.9},
                {252.02, -17.1},
                {256.53, -4.5},
                {261.02, -9},
                {265.51, -33},
                {270.02, -21.9},
                {274.51, -18.9},
                {279.02, -8.7},
                {283.5, -21.3},
                {288.01, -9},
                {292.51, -21.3},
                {297.01, -23.4},
                {301.51, -6.3},
                {306.01, -18.6},
                {310.52, -30},
                {315.01, -27.3},
                {319.5, -27},
                {324.02, -46.2},
                {328.53, -30.6},
                {333.01, -9.3},
                {337.51, -38.1},
                {342.02, -33.3},
                {346.51, -52.2},
                {351.01, -39.9},
                {355.5, -49.5},
            };


            var data = M.DenseOfArray(rawData);
            var ones = V.Dense(data.RowCount, 1);

            // vector of angle in radians
            var angRad = data.Column(0) * (Math.PI / 180.0);

            // vector of heights, shifted for zero mean
            var height = data.Column(1);
            var offset = height.DotProduct(ones) / height.Count;
            var heightShifted = height + offset;

            var w = angRad.PointwiseSin();
            var z = angRad.PointwiseCos();
            var X = M.DenseOfColumnVectors(w, z);

            var B = (X.Transpose() * X).Inverse() * X.Transpose() * heightShifted;

            var A = Math.Sqrt(B[0]*B[0] + B[1]*B[1]);
            var phi = Math.Atan(B[1]/B[0]);

            Debug.Print($"{A}, {phi}");

        }

    }
}
