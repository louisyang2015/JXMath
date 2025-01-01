using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JXMath
{
    public static class Globals
    {
        #region double[]

        /// <summary>
        /// Add two arrays element by element.
        /// </summary>
        public static double[] Add(this double[] a, double[] b)
        {
            if (a.Length != b.Length)
                throw new ArgumentException("Arrays must have the same length.");

            double[] v = new double[a.Length];
            for (int i = 0; i < a.Length; i++)
                v[i] = a[i] + b[i];

            return v;
        }


        /// <summary>
        /// Create an array of "length" elements, all initialized to "value".
        /// </summary>
        public static double[] MakeArray(double length, double value)
        {
            double[] result = new double[(int)length];

            for (int i = 0; i < length; i++)
                result[i] = value;

            return result;
        }


        /// <summary>
        /// Make an array of doubles starting at "start", incrementing by "increment", 
        /// and ending at "end". The "end" value is included in the array.
        /// </summary>
        public static double[] MakeArray(double start, double increment, double end)
        {
            int n = (int)((end - start) / increment) + 1;
            double[] result = new double[n];

            for (int i = 0; i < n; i++)
                result[i] = start + i * increment;

            return result;
        }


        /// <summary>
        /// Multiply two arrays element by element.
        /// </summary>
        public static double[] Mult(this double[] x, double[] y)
        {
            if (x.Length != y.Length)
                throw new ArgumentException("Array dimensions do not match.");

            double[] result = new double[x.Length];

            for (int i = 0; i < x.Length; i++)
                result[i] = x[i] * y[i];

            return result;
        }


        /// <summary>
        /// Returns the L2 norm of the array.
        /// </summary>
        public static double Norm(this double[] x)
        {
            double sum = 0;

            for (int i = 0; i < x.Length; i++)
                sum += x[i] * x[i];

            return Math.Sqrt(sum);
        }


        /// <summary>
        /// Normalize the array using the L2 norm.
        /// </summary>
        public static void Normalize(this double[] x)
        {
            double norm = x.Norm();

            for (int i = 0; i < x.Length; i++)
                x[i] /= norm;
        }


        /// <summary>
        /// Multiply all elements of the array by "factor".
        /// </summary>
        public static void Scale(this double[] x, double factor)
        {
            for (int i = 0; i < x.Length; i++)
                x[i] *= factor;
        }

        #endregion


        #region Func

        public static double[] Eval(this Func<double, double> func, double[] x)
        {
            double[] result = new double[x.Length];

            for (int i = 0; i < x.Length; i++)
            {
                result[i] = func(x[i]);

                if (double.IsNaN(result[i]) || double.IsInfinity(result[i]))
                    throw new Exception($"Function evaluation failed for x={x[i]}.");
            }

            return result;
        }
        
        #endregion



        #region Probability

        public static double Likelihood(double[][] data, IProbabilityModel model)
        {
            double likelihood = 1;

            for (int i = 0; i < data.Length; i++)
            {
                likelihood *= model.Probability(data[i]);
            }

            return likelihood;
        }

        public static double[] Likelihood(double[][] data, IProbabilityModel[] models)
        {
            var result = new double[models.Length];

            for (int i = 0; i < models.Length; i++)
                result[i] = Likelihood(data, models[i]);

            return result;
        }

        #endregion
    }

}
