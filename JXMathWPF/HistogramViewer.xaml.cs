using JXMath;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace JXMathWPF
{
    /// <summary>
    /// Interaction logic for HistogramViewer.xaml
    /// </summary>
    public partial class HistogramViewer : Window
    {
        Histogram _histogram;
        Brush _brush = Brushes.Blue; // color of the histogram bars

        // Bar highlighting
        Brush _highlight_brush = Brushes.Orange;
        HistogramBar? _highlighted_bar = null;

        public HistogramViewer(Histogram histogram, Brush? brush = null, Brush? highlight_brush = null)
        {
            _histogram = histogram;

            if (brush != null)
                _brush = brush;

            if (highlight_brush != null)
                _highlight_brush = highlight_brush;

            InitializeComponent();
        }


        /// <summary>
        /// Copy from member variables to GUI
        /// </summary>
        void UpdateGUI()
        {
            Low_tb.Text = _histogram.Low.ToString();
            High_tb.Text = _histogram.High.ToString();
            NumBins_tb.Text = _histogram.NumBins.ToString();
        }


        void DrawHistogram()
        {
            Histogram_canvas.ClearVisuals();

            // Find max value in histogram
            double max_count = 0;
            for (int i = 0; i < _histogram.NumBins; i++)
            {
                if (_histogram[i] > max_count)
                    max_count = _histogram[i];
            }

            if (LogHeight_cb.IsChecked == true)
                max_count = Math.Log(max_count + 1);

            // Draw histogram bin by bin
            var pen = new Pen(_brush, 1);

            for (int i = 0; i < _histogram.NumBins; i++)
            {
                var bar = new HistogramBar(_histogram, i, Histogram_canvas.ActualWidth, 
                    Histogram_canvas.ActualHeight, max_count, LogHeight_cb.IsChecked == true, 
                    _brush, pen);
                Histogram_canvas.AddVisual(bar);
            }
        }

        void RemoveOldHighlight()
        {
            if (_highlighted_bar != null)
            {
                _highlighted_bar.ReColor(_brush);
                _highlighted_bar = null;
            }
        }

        // ---------- Event handlers ----------

        private void Histogram_canvas_MouseMove(object sender, MouseEventArgs e)
        {
            Point p = e.GetPosition(Histogram_canvas);

            var visual = Histogram_canvas.GetVisual(p);

            if (visual is HistogramBar bar)
            {
                if (bar != _highlighted_bar)
                {
                    RemoveOldHighlight();
                    bar.ReColor(_highlight_brush);
                    _highlighted_bar = bar;
                    Status_tb.Text = bar.Label;
                }                
            }
            else
            {
                RemoveOldHighlight();
                Status_tb.Text = "";
            }
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            // Read from GUI -> "low", "high", and "num_bins"
            double? low = null, high = null;
            int? num_bins = null;

            if (double.TryParse(Low_tb.Text, out double temp))
                low = temp;

            if (double.TryParse(High_tb.Text, out temp))
                high = temp;

            if (int.TryParse(NumBins_tb.Text, out int temp2))
                num_bins = temp2;

            _histogram.ReCompute(low, high, num_bins);            

            UpdateGUI();
            DrawHistogram();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateGUI();
            DrawHistogram();
        }
        
    }


    /// <summary>
    /// A rectangle representing a single bar in the histogram
    /// </summary>
    class HistogramBar : DrawingVisual
    {
        public string Label { get; private set; }
        Rect _location;
        
        /// <param name="index">the bin index for this bar</param>
        /// <param name="canvas_width">the width of the overall drawing area</param>
        /// <param name="canvas_height">the height of the overall drawing area</param>
        /// <param name="max_count">largest bin in the histogram. This value has already 
        /// gone through the "log" function if that's needed.</param>
        /// <param name="use_log">If true, apply the log function to the count</param>
        public HistogramBar(Histogram histogram, int index, double canvas_width, double canvas_height,
            double max_count, bool use_log, Brush brush, Pen pen)
        {
            // Label
            if (histogram.BinWidth == 1)
            {
                Label = $"Value = {histogram.Low + index}; Count = {histogram[index]}";
            }
            else
            {
                char end_char = ')'; // usually exclusive end, unless it's the final bin
                if (index == histogram.NumBins - 1)
                    end_char = ']';
                
                double low = histogram.Low + index * histogram.BinWidth;
                double high = low + histogram.BinWidth;

                Label = $"Values = [{low} ~ {high}{end_char}; Count = {histogram[index]}";
            }

            // Determine location
            double count = histogram[index];
            if (use_log)
                count = Math.Log(count + 1);

            double bar_height = canvas_height * count / max_count;
            double bar_width = canvas_width / histogram.NumBins;
            double x = index * bar_width;
            double y = canvas_height - bar_height;

            _location = new Rect(x, y, bar_width, bar_height);

            // Draw
            using var dc = RenderOpen();
            dc.DrawRectangle(brush, pen, _location);
        }

        
        public void ReColor(Brush brush)
        {
            Pen pen = new Pen(brush, 1);
            using var dc = RenderOpen();
            dc.DrawRectangle(brush, pen, _location);
        }
    }

}
