using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static System.Math;
using static JXMath.Globals;


namespace JXMath
{
    public class Histogram
    {
        public double Low { get; private set; }
        public double High { get; private set; }
        
        int[] _histogram;

        public int NumBins
        {
            get { return _histogram.Length; }
        }

        public double BinWidth
        {
            get { return (High - Low) / NumBins; }
        }

        public int this[int i]
        {
            get { return _histogram[i]; }
        }


        public void ReCompute(double? num_bins = null)
        {
            if (_data_bytes_2d.Length > 0)
                ReCompute_Bytes2D(num_bins);

            if (_data_double.Length > 0)
                ReCompute_Double(num_bins);            
        }

        public void ReCompute(double? low = null, double? high = null, int? num_bins = null)
        {
            if (_data_bytes_2d.Length > 0)
                ReCompute_Bytes2D(low, high, num_bins);

            if (_data_double.Length > 0)
                ReCompute_Double(low, high, num_bins);
        }


        #region Implementation for double[]

        double[] _data_double = new double[0];

        public Histogram(double[] data)
        {
            _data_double = data;

            // Default "Low" and "High"
            Low = data[0];
            High = data[0];

            for (int i = 1; i < data.Length; i++)
            {
                if (data[i] < Low)
                    Low = data[i];
                if (data[i] > High)
                    High = data[i];
            }

            // Default number of bins
            int num_bins = (int)Math.Sqrt(data.Length);

            if (num_bins < 3)
                num_bins = 3;

            if (num_bins > 101)
                num_bins = 101;

            // Prefer odd number of bins so to see the median easily
            if (num_bins % 2 == 0)
                num_bins++;

            _histogram = new int[num_bins];

            ReCompute_Double();
        }

        void ReCompute_Double(double? low = null, double? high = null, int? num_bins = null)
        {
            RefreshHistogramParams(low, high, num_bins);

            // cache some values for hopefully better performance
            double bin_width = BinWidth;
            int last_bin_number = NumBins - 1;

            var data = _data_double;

            foreach (double x in data)
            {
                if (x < Low || x > High)
                    continue;

                if (x == High)
                    _histogram[last_bin_number]++;
                else
                {
                    int bin = (int)((x - Low) / bin_width);
                    _histogram[bin]++;
                }
            }
        }


        /// <summary>
        /// Refresh histogram parameters as needed.
        /// </summary>
        void RefreshHistogramParams(double? low = null, double? high = null, int? num_bins = null)
        {
            // Update "Low", "High", and "_histogram" as needed
            if (low.HasValue)
                Low = low.Value;

            if (high.HasValue)
                High = high.Value;

            // Re-allocate "_histogram" if needed
            if (num_bins.HasValue)
            {
                _histogram = new int[num_bins.Value];
            }
            else
            {
                // Reset to zero
                for (int i = 0; i < _histogram.Length; i++)
                    _histogram[i] = 0;
            }
        }

        #endregion



        #region Implementation for byte[,]

        byte[,] _data_bytes_2d = new byte[0, 0];

        public Histogram(byte[,] data)
        {
            _data_bytes_2d = data;

            // Default "Low" and "High"
            Low = data[0, 0];
            High = data[0, 0];

            int width = data.GetLength(0);
            int height = data.GetLength(1);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (data[x, y] < Low)
                        Low = data[x, y];

                    if (data[x, y] > High)
                        High = data[x, y];
                }
            }

            // Default number of bins
            int num_bins = (int)High - (int)Low + 1;

            _histogram = new int[num_bins];

            ReCompute_Bytes2D();
        }

        void ReCompute_Bytes2D(double? low = null, double? high = null, int? num_bins = null)
        {
            RefreshHistogramParams(low, high, num_bins);

            // cache some values for hopefully better performance
            double bin_width = BinWidth;
            int last_bin_number = NumBins - 1;

            var data = _data_bytes_2d;

            foreach (double x in data)
            {
                if (x < Low || x > High)
                    continue;

                if (x == High)
                    _histogram[last_bin_number]++;
                else
                {
                    int bin = (int)((x - Low) / bin_width);
                    _histogram[bin]++;
                }
            }
        }

        #endregion


        #region GMM Estimation

        /// <summary>
        /// Returns arrays of (mean, variance, weight) estimates for Gaussian 
        /// Mixture Model using the vertical sweep method.
        /// </summary>
        /// <param name="minGap">This is the distance between a peak and a nearby 
        /// different interval. The unit for this distance is the number of bins.</param>
        public (double[] mean, double[] variance, double[] weights) Get_GMM_Estimate(
            int? minGap = null)
        {
            // Minimum gap between intervals is 5% of the total number of bins
            int min_gap = (int)(_histogram.Length / 100.0 * 5.0);
            if (min_gap < 1)
                min_gap = 1;

            if (minGap.HasValue)
                min_gap = minGap.Value;

            // Clone the histogram -> y_values
            // Sort the y_values from largest to smallest
            var y_values = new int[_histogram.Length];
            Array.Copy(_histogram, y_values, _histogram.Length);
            Array.Sort(y_values);
            Array.Reverse(y_values);

            // Each bin gets an "interval" number
            var interval = new short[_histogram.Length];

            // Initially assign all bins to an interval number of -1
            for (int i = 0; i < interval.Length; i++)
                interval[i] = -1;

            // Vertical scan "_histogram"
            short num_intervals = 0;

            // Track how many bins and values remain unassigned
            int bins_left = _histogram.Length;
            int total_values = _histogram.Sum();
            int values_left = total_values;

            for (int i = 0; i < y_values.Length; i++)
            {
                // Check for termination if the "y_value" has dropped sufficiently low
                double avg_height = (double)values_left / bins_left;

                if (y_values[i] < avg_height * 2)
                    break;

                // Handle all bins with y_values[i]
                for (int j = 0; j < interval.Length; j++)
                {
                    if (_histogram[j] == y_values[i])
                    {
                        // Assign the bin to an interval
                        var result = AssignBinToInterval(interval, j, min_gap, ref num_intervals);
                        bins_left -= result.binsAssigned;
                        values_left -= result.valuesAssigned;
                    }
                }
            }

            // Compute mean and variance for the intervals
            var means = new double[num_intervals];
            var variances = new double[num_intervals];
            var weights = new double[num_intervals];

            for (int i = 0; i < num_intervals; i++)
            {
                var stats = Compute_Stats_for_Interval(interval, i);
                means[i] = stats.mean;
                variances[i] = stats.variance;
                weights[i] = (double)stats.numValues / total_values;
            }

            weights.Scale(1.0 / weights.Sum());

            return (means, variances, weights);
        }


        /// <summary>
        /// Assign interval[index] to an interval number. If the interval[index]
        /// is at least "minGap" away from the closest interval, then assign 
        /// a new interval number. Returns the number of bins and values assigned.
        /// </summary>
        (int binsAssigned, int valuesAssigned) AssignBinToInterval(short[] interval, 
            int index, int minGap, ref short numIntervals)
        {
            // If the position is already assigned to an interval, then return
            if (interval[index] != -1)
                return (0, 0);

            int values_assigned = 0;

            // See if there's other intervals nearby
            for (int i = 0; i <= minGap; i++)
            {
                if (index + i < interval.Length
                    && interval[index + i] != -1)
                {
                    // interval[index ... index + i] is the same interval
                    for (int j = 0; j < i; j++)
                    {
                        interval[index + j] = interval[index + i];
                        values_assigned += _histogram[index + j];
                    }

                    // bins_assigned = i;
                    return (i, values_assigned);
                }

                if (index - i >= 0
                    && interval[index - i] != -1)
                {
                    // interval[index - i ... index] is the same interval
                    for (int j = 0; j < i; j++)
                    {
                        interval[index - j] = interval[index - i];
                        values_assigned += _histogram[index + j];
                    }

                    // bins_assigned = i;
                    return (i, values_assigned);
                }
            }

            // Code arrives here if the point at "index" is a new interval
            interval[index] = numIntervals;
            numIntervals++;

            return (1, _histogram[index]);
        }


        /// <summary>
        /// Compute mean and variance for a certain interval
        /// </summary>
        (double mean, double variance, int numValues) Compute_Stats_for_Interval(
            short[] interval, int intervalNumber)
        {
            // Determine the mean first
            double total = 0;
            double bin_width = BinWidth;
            int num_values = 0;

            for (int i = 0; i < interval.Length; i++)
            {
                if (interval[i] == intervalNumber)
                {
                    total += _histogram[i] * (Low + i * bin_width);
                    num_values += _histogram[i];
                }
            }

            double mean = total / num_values;

            // Determine the variance
            total = 0;

            for (int i = 0; i < interval.Length; i++)
            {
                if (interval[i] == intervalNumber)
                {
                    total += _histogram[i] * Pow((Low + i * bin_width) - mean, 2);
                }
            }

            double variance = total / num_values;

            return (mean, variance, num_values);
        }

        #endregion


        public string ToTsv()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Bin\tFrequency");

            for (int i = 0; i < NumBins; i++)
            {
                sb.AppendLine($"[{Low + i * BinWidth} ~ {Low + (i + 1) * BinWidth})\t{_histogram[i]}");

                // Last bin is inclusive
                if (i == NumBins - 1)
                    sb.AppendLine($"[{Low + i * BinWidth} ~ {Low + (i + 1) * BinWidth}]\t{_histogram[i]}");
            }

            return sb.ToString();
        }

    }
}
