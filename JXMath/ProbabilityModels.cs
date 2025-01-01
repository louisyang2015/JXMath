using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JXMath
{
    public interface IProbabilityModel
    {
        double Probability(double[] inputs);
    }


    public class NormalDistribution
    {
        double _mean, _stddev;

        public NormalDistribution(double mean, double stddev)
        {
            _mean = mean;
            _stddev = stddev;
        }

        public double PDF(double x)
        {
            return Math.Exp(-0.5 * Math.Pow((x - _mean) / _stddev, 2)) / (_stddev * Math.Sqrt(2 * Math.PI));
        }
    }

}
