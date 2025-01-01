using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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



        #region Implementation for double[]

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
