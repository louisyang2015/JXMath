using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static System.Math;
using static JXMath.GlobalRandom;


namespace JXMath
{
    interface IRandomVariable
    {
        public double Sample();
    }


    static class GlobalRandom
    {
        static Random _rand = new Random();

        public static double NextDouble()
        {
            return _rand.NextDouble();
        }
    }


    public class RandomVariableSet
    {
        // A set of probabilities
        List<IRandomVariable> _rv = new List<IRandomVariable>();
        List<double> _likelihood = new List<double>(); // does not add up to 1
        double[] _cdf = []; // _cdf[i] leads to _rv[i]


        public double Sample()
        {
            // Figure out which of the "_prob" to use
            double r = NextDouble();

            for (int i = 0; i < _cdf.Length; i++)
            {
                if (r < _cdf[i])
                    return _rv[i].Sample();
            }

            throw new Exception("Unable to select a probability to use.");
        }


        public double[] Sample(int length)
        {
            var data = new double[length];
            for (int i = 0; i < length; i++)
                data[i] = Sample();

            return data;
        }


        public RandomVariableSet Add_UniformRV(double low, double high, double likelihood = 1.0)
        {
            _rv.Add(new UniformRV(low, high));
            _likelihood.Add(likelihood);

            RebuildCDF();
            return this;
        }


        public RandomVariableSet Add_NormalRV(double mean, double stddev, double likelihood = 1.0)
        {
            _rv.Add(new NormalRV(mean, stddev));
            _likelihood.Add(likelihood);
            RebuildCDF();
            return this;
        }


        /// <summary>
        /// Refills the "_cdf" array, which decides which "_rv" (random variable)
        /// to use when the Sample() method is called.
        /// </summary>
        void RebuildCDF()
        {
            double total_prob = 0;

            for (int i = 0; i < _likelihood.Count; i++)
                total_prob += _likelihood[i];

            _cdf = new double[_likelihood.Count];

            double sum = 0;

            for (int i = 0; i < _likelihood.Count; i++)
            {
                sum += _likelihood[i] / total_prob;
                _cdf[i] = sum;
            }
        }
    }


    /// <summary>
    /// Implements a uniform distributed random variable.
    /// </summary>
    class UniformRV : IRandomVariable
    {
        private double _low;
        private double _high;

        public UniformRV(double low, double high)
        {
            _low = low;
            _high = high;
        }

        public double Sample()
        {
            return _low + (_high - _low) * NextDouble();
        }
    }


    /// <summary>
    /// Implements a normal distributed random variable.
    /// </summary>
    class NormalRV : IRandomVariable
    {
        private double _mean;
        private double _stddev;

        // The Box–Muller Method generates two random numbers at a time
        double? z2 = null;

        public NormalRV(double mean, double stddev)
        {
            _mean = mean;
            _stddev = stddev;
        }


        public double Sample()
        {
            if (z2.HasValue)
            {
                double z = z2.Value;
                z2 = null;
                return _mean + _stddev * z;
            }

            // Box–Muller Method
            double u1 = NextDouble();
            double u2 = NextDouble();
            double z1 = Sqrt(-2.0 * Log(u1)) * Cos(2.0 * PI * u2);
            z2 = Sqrt(-2.0 * Log(u1)) * Sin(2.0 * PI * u2);

            return _mean + _stddev * z1;
        }
    }


}
