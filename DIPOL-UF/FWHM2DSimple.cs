#nullable enable
using System;
using System.Buffers;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.Optimization;
using Microsoft.Toolkit.HighPerformance;


namespace DIPOL_UF
{
    internal class FWHM2DSimple
    {
        public class FitParams
        {
            public double Scale { get; init; }
            public double Sigma { get; init; }
            public double X0 { get; init; }
            public double Y0 { get; init; }
            public double ZeroPoint { get; init; }

            public double FWHM => MathHelper.FWHMConst * Sigma;
            public Vector<double> ToVector()
            {
                Vector<double> v = Vector<double>.Build.Dense(5);
                v[0] = Scale;
                v[1] = Sigma;
                v[2] = X0;
                v[3] = Y0;
                v[4] = ZeroPoint;
                return v;
            }

            public static FitParams FromVector(Vector<double> data) =>
                data.Count != 5
                    ? throw new ArgumentException()
                    : new FitParams
                    {
                        Scale = data[0],
                        Sigma = data[1],
                        X0 = data[2],
                        Y0 = data[3],
                        ZeroPoint = data[4]
                    };
        }

        public static FitParams ComputeFullWidthHalfMax2D(ReadOnlySpan2D<float> data)
        {
            if (data.IsEmpty)
            {
                return new FitParams();
            }

            try
            {
                var (width, height) = (data.Width, data.Height);
                double[] x = Generate.LinearRange(0, width - 1);
                double[] y = Generate.LinearRange(0, height - 1);
                float[] dataAlloc = new float[data.Length];
                if (!data.TryCopyTo_Temp(dataAlloc))
                {
                    return new FitParams();
                }

                var (min, max) = data.MinMax();
                var fun = ObjectiveFunction.Value(v => Dist2D(FitParams.FromVector(v), x, y, dataAlloc));
                var @params = new FitParams
                {
                    Scale = max,
                    Sigma = x.Length / 8.0f + y.Length / 8.0f,
                    X0 = x.Length / 2.0f,
                    Y0 = y.Length / 2.0f,
                    ZeroPoint = min
                };

                var fitResult = NelderMeadSimplex.Minimum(
                    fun,
                    @params.ToVector(),
                    convergenceTolerance: 1e-6,
                    maximumIterations: 5_000
                );
                return FitParams.FromVector(fitResult.MinimizingPoint);
            }
            catch (Exception e)
            {
                if (Injector.GetLogger() is { } logger)
                {
                    logger.Error(e, "Failed fitting using {Method}.", nameof(FWHM2D));
                }
                
                return new FitParams();
            }
        }

        private static double ExpFunc2D(FitParams p, double x, double y)
        {
            var r2 = (x - p.X0) * (x - p.X0) + (y - p.Y0) * (y - p.Y0);
            return
                p.ZeroPoint
                + p.Scale // / (2 * Math.PI * p.Sigma1 * p.Sigma2)
                * Math.Exp(
                    -r2 / 2 / p.Sigma / p.Sigma
                );
        }

        private static double Dist2D(FitParams p, ReadOnlySpan<double> x, ReadOnlySpan<double> y, float[] data, double[]? buffer = null)
        {
            if (data.Length != x.Length * y.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(data));
            }

            if (buffer is null)
            {
                buffer = new double[data.Length];
            }

            MathHelper.GridExpand(x, y, new Span2D<double>(buffer, y.Length, x.Length), (lx, ly) => ExpFunc2D(p, lx, ly));

            return MathHelper.DistanceEuclidean(buffer, data);
            // return Distance.Euclidean(data, buffer);
        }

        
        
    }
}