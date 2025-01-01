using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using JXMathWPF.Common;

using static System.Math;


namespace JXMathWPF
{
    /// <summary>
    /// Interaction logic for XYPlot.xaml
    /// </summary>
    public partial class XYPlot : Window
    {
        // Data
        List<DataSet> _data = [];
        List<string> _names = [];

        // GUI state
        bool _disable_gui = true;
        ViewPort _view_port = new ViewPort();
        GraphSettings _settings = new GraphSettings();
        PlotPipeLine _pipeline;


        public XYPlot()
        {
            InitializeComponent();
            _pipeline = new PlotPipeLine(_settings, _view_port);
        }


        public XYPlot AddData(double[] x, double[] y, string name, string color = "Blue")
        {
            _data.Add(new DataSet(x, y, color));

            name = name.Trim();
            if (name == "")
                name = $"Data ({_data.Count})";
            _names.Add(name);

            return this;
        }


        void RedrawGraph()
        {
            XYGraph_canvas.ClearVisuals();

            // Update zoom level if needed
            if (double.TryParse(ZoomX_tb.Text, out double zoom))
                _view_port.ZoomX = zoom;

            if (double.TryParse(ZoomY_tb.Text, out zoom))
                _view_port.ZoomY = zoom;

            ZoomX_tb.Text = _view_port.ZoomX.ToString("g5");
            ZoomY_tb.Text = _view_port.ZoomY.ToString("g5");

            _settings.Width = XYGraph_canvas.ActualWidth;
            _settings.Height = XYGraph_canvas.ActualHeight;

            // For each selected data set, get visuals
            var selected_data = new List<DataSet>();

            foreach (var item in DataSets_lb.SelectedItems)
            {
                int index = DataSets_lb.Items.IndexOf(item);
                selected_data.Add(_data[index]);
            }

            var visuals = _pipeline.GetVisuals(selected_data.ToArray());
            XYGraph_canvas.AddVisuals(visuals);
        }


        #region Event Handlers

        private void DataSets_lb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_disable_gui) return;

            _view_port.Reset(_data, DataSets_lb);
            RedrawGraph();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Left)
                _view_port.Move("left");
            else if (e.Key == Key.Right)
                _view_port.Move("right");
            else if (e.Key == Key.Up)
                _view_port.Move("up");
            else if (e.Key == Key.Down)
                _view_port.Move("down");

            RedrawGraph();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _disable_gui = true;

            // Initialize "DataSets_lb"
            DataSets_lb.Items.Clear();
            foreach (string name in _names)
                DataSets_lb.Items.Add(name);

            DataSets_lb.SelectedIndex = 0;

            // Initialize view port
            _view_port.Reset(_data, DataSets_lb);

            RedrawGraph();

            _disable_gui = false;
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_disable_gui) return;

            RedrawGraph();
        }

        private void XYGraph_canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {

        }

        private void XYGraph_canvas_MouseMove(object sender, MouseEventArgs e)
        {

        }

        private void Zoom_tb_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                // Check user input
                if (!double.TryParse(ZoomX_tb.Text, out double zoom))
                {
                    MessageBox.Show($"Invalid zoom X level '{ZoomX_tb.Text}'.");
                    return;
                }

                if (!double.TryParse(ZoomY_tb.Text, out zoom))
                {
                    MessageBox.Show($"Invalid zoom Y level '{ZoomY_tb.Text}'.");
                    return;
                }

                RedrawGraph();
            }
        }

        #endregion


        #region Inner Classes

        class GraphSettings
        {
            public double Width = 200;
            public double Height = 100;

            public bool LogScale_X = false;
            public bool LogScale_Y = false;

            public double CircleRadius = 8+2; // Diameter 16 means 96 / 16 = 6 points per inch
        }


        class DataSet
        {
            // The pair (X, Y) is sorted by X by the end of construction
            public double[] X { get; private set; }
            public double[] Y { get; private set; }

            public Color Color { get; private set; }

            // After construction, the "X" is sorted
            public double XMin { get => X[0]; }
            public double XMax { get => X[X.Length - 1]; }
            public double YMin { get; }
            public double YMax { get; }

            /// <summary>
            /// Log is applied when needed.
            /// </summary>
            public bool _log_scale_x { get; private set; } = false;
            public bool _log_scale_y { get; private set; } = false;

            Dictionary<string, string> _color_shortcut = new Dictionary<string, string>()
        {
            { "b", "Blue" },
            { "c", "Cyan" },
            { "g", "Green" },
            { "m", "Magenta" },
            { "r", "Red" },
            { "v", "Violet" },
            { "y", "Yellow" },
        };


            public DataSet(double[] x, double[] y, string color)
            {
                X = x;
                Y = y;

                YMin = y.Min();
                YMax = y.Max();

                if (_color_shortcut.ContainsKey(color.ToLower()))
                    color = _color_shortcut[color];

                try
                {
                    Color = (Color)ColorConverter.ConvertFromString(color);
                }
                catch
                {
                    throw new Exception($"Invalid color: '{color}'.");
                }

                Check_and_Duplicate_Data();
            }


            /// <summary>
            /// Checks the "X" and "Y" data arrays to be the same length and sorted by X. 
            /// Duplicate the data so there's no chance of corrupting the data from
            /// the caller.
            /// </summary>
            void Check_and_Duplicate_Data()
            {
                bool duplicated = false;

                // Check that X and Y are the same length
                if (X.Length != Y.Length)
                {
                    // Fix the length by allocating new arrays
                    int new_length = Math.Min(X.Length, Y.Length);
                    double[] new_x = new double[new_length];
                    double[] new_y = new double[new_length];

                    for (int i = 0; i < new_length; i++)
                    {
                        new_x[i] = X[i];
                        new_y[i] = Y[i];
                    }

                    X = new_x;
                    Y = new_y;
                    duplicated = true;
                }

                if (!duplicated)
                {
                    X = (double[])X.Clone();
                    Y = (double[])Y.Clone();
                }

                // Data is duplicated by now

                // Sorting by X makes filtering more efficient
                // Check that X is sorted
                bool sorted = true;

                for (int i = 0; i < X.Length - 1; i++)
                {
                    if (X[i] > X[i + 1])
                    {
                        sorted = false;
                        break;
                    }
                }

                if (!sorted)
                    Array.Sort(X, Y);
            }


            /// <summary>
            /// Apply (or remove) log scale to "x" or "y" axis.
            /// </summary>
            public void ApplyLog(string axis, bool flag)
            {
                if (axis == "x" && _log_scale_x == flag) return;
                if (axis == "y" && _log_scale_y == flag) return;

                // Code arrive here if action is needed

                // Point "array" to the right array
                var array = X;
                if (axis == "y")
                    array = Y;

                // Apply (or remove) log scale
                if (flag)
                {
                    for (int i = 0; i < array.Length; i++)
                        array[i] = Log10(array[i]);
                }
                else
                {
                    for (int i = 0; i < array.Length; i++)
                        array[i] = Pow(10, array[i]);
                }

                // Sync the flag
                if (axis == "x")
                    _log_scale_x = flag;
                else
                    _log_scale_y = flag;
            }
        }


        /// <summary>
        /// Tracks what is currently being viewed in the graph.
        /// </summary>
        class ViewPort
        {
            public double CenterX { get; private set; }
            public double CenterY { get; private set; }

            double _zoomX = 1;
            public double ZoomX
            {
                get => _zoomX;
                set
                {
                    if (value >= 1)
                        _zoomX = value;
                }
            }

            double _zoomY = 1;
            public double ZoomY
            {
                get => _zoomY;
                set
                {
                    if (value >= 1)
                        _zoomY = value;
                }
            }

            public double XMin { get; private set; }
            public double XMax { get; private set; }
            public double YMin { get; private set; }
            public double YMax { get; private set; }

            /// <summary>
            /// Reset view port, based on the full "data" set, and the selections made in 
            /// "listbox".
            /// </summary>
            public void Reset(List<DataSet> data, ListBox listbox)
            {
                CenterX = 0;
                CenterY = 0;
                ZoomX = 1;
                ZoomY = 1;

                XMin = 0;
                XMax = 0;
                YMin = 0;
                YMax = 0;

                if (listbox.SelectedItems.Count == 0)
                    return;

                // XMin, XMax, YMin, YMax
                bool first_item = true;

                foreach (var item in listbox.SelectedItems)
                {
                    int index = listbox.Items.IndexOf(item);
                    var d = data[index];

                    if (first_item)
                    {
                        XMin = d.XMin;
                        XMax = d.XMax;
                        YMin = d.YMin;
                        YMax = d.YMax;
                        first_item = false;
                    }
                    else
                    {
                        XMin = Min(XMin, d.XMin);
                        XMax = Max(XMax, d.XMax);
                        YMin = Min(YMin, d.YMin);
                        YMax = Max(YMax, d.YMax);
                    }
                }

                // Add in some margin
                double x_margin = 0.05 * (XMax - XMin);
                double y_margin = 0.05 * (YMax - YMin);

                XMin -= x_margin;
                XMax += x_margin;
                YMin -= y_margin;
                YMax += y_margin;

                // CenterX, CenterY
                CenterX = (XMin + XMax) / 2.0;
                CenterY = (YMin + YMax) / 2.0;
            }

            public void Move(string direction)
            {
                double zoom_factor = 1.0 / ZoomX;
                double dx = (XMax - XMin) * zoom_factor * 0.1;

                zoom_factor = 1.0 / ZoomY;
                double dy = (YMax - YMin) * zoom_factor * 0.1;

                if (direction == "up")
                    CenterY += dy;
                else if (direction == "down")
                    CenterY -= dy;
                else if (direction == "left")
                    CenterX -= dx;
                else if (direction == "right")
                    CenterX += dx;
            }
        }


        /// <summary>
        /// Translate data points to screen coordinates.
        /// </summary>
        class PlotPipeLine
        {
            // Use a dictionary to map multiple data points to a single visual
            Dictionary<(int, int), CircleVisual> _visuals = [];
            int _visuals_width = 200; // max number of points horizontally
            int _visuals_height = 100; // max number of points vertically

            GraphSettings _settings;
            ViewPort _view;


            public PlotPipeLine(GraphSettings settings, ViewPort view)
            {
                _settings = settings;
                _view = view;
            }


            /// <summary>
            /// Compute a set of visuals for the given "data".
            /// </summary>
            public List<Visual> GetVisuals(params DataSet[] data)
            {
                _visuals.Clear();

                // Update "_width" and "_height"
                _visuals_width = (int)(_settings.Width / 96.0 * 6); // 8 points per inch
                _visuals_height = (int)(_settings.Height / 96.0 * 6);

                // The "_visuals" is a dictionary to group points that are close to
                // each other. This is to reduce the number of visuals to be drawn.
                // Compute the step size for the "_visuals" grid
                double _visuals_x_step = (_view.XMax - _view.XMin) / _visuals_width;
                double _visuals_y_step = (_view.YMax - _view.YMin) / _visuals_height;

                foreach (var d in data)
                {
                    // Apply log scale if needed
                    d.ApplyLog("x", _settings.LogScale_X);
                    d.ApplyLog("y", _settings.LogScale_Y);

                    // Determine the range of x-indices that are within the view port
                    (int x_left, int x_right) = Determine_X_Index_Range(d.X);

                    for (int x = x_left; x <= x_right; x++)
                    {
                        // Filter out Y values that are outside the view port
                        if (d.Y[x] < _view.YMin || d.Y[x] > _view.YMax)
                            continue;

                        // Code arrive here if the data point is within the view port
                        // For each data point in the view port, create a visual object

                        int visual_x = Round((d.X[x] - _view.XMin) / _visuals_x_step);
                        int visual_y = Round((d.Y[x] - _view.YMin) / _visuals_y_step);

                        if (visual_x >= 0 && visual_x < _visuals_width
                            && visual_y >= 0 && visual_y < _visuals_width)
                        {
                            Add_Data_Point_To_Visuals(visual_x, visual_y, d, x);
                        }
                    }
                }

                var visuals = new List<Visual>();
                foreach (var visual in _visuals.Values)
                    visuals.Add(visual);

                return visuals;
            }


            #region Helpers


            void Add_Data_Point_To_Visuals(int visual_x, int visual_y, DataSet data, int index)
            {
                if (_visuals.ContainsKey((visual_x, visual_y)))
                {
                    _visuals[(visual_x, visual_y)].Flag_As_Multiple_Points();
                    return;
                }

                double data_x = data.X[index];
                double data_y = data.Y[index];

                // Translate (data_x, data_y) to screen coordinates
                double screen_x = (data_x - _view.XMin) / (_view.XMax - _view.XMin) * _settings.Width;
                double screen_y = _settings.Height - (data_y - _view.YMin) / (_view.YMax - _view.YMin) * _settings.Height;

                // Create a new visual
                var visual = new CircleVisual(new Point(screen_x, screen_y), _settings.CircleRadius,
                    data.Color, data_x, data_y);
                _visuals.Add((visual_x, visual_y), visual);
            }


            /// <summary>
            /// Given an array of sorted "x" data, determine the range of indices 
            /// that are within the view port settings.
            /// </summary>
            (int x_left, int x_right) Determine_X_Index_Range(double[] sorted_x_data)
            {
                // The (X, Y) data is sorted by X

                // Do binary search on "X" to find the data points with "X" values that are
                // within the view port

                // x_left index
                int x_left = 0;

                if (sorted_x_data[0] < _view.XMin)
                {
                    x_left = Array.BinarySearch(sorted_x_data, _view.XMin);
                    if (x_left < 0)
                        x_left = ~x_left;
                }

                // x_right index
                int x_right = sorted_x_data.Length - 1;

                if (sorted_x_data[x_right] > _view.XMax)
                {
                    x_right = Array.BinarySearch(sorted_x_data, _view.XMax);
                    if (x_right < 0)
                        x_right = ~x_right - 1;
                }

                return (x_left, x_right);
            }


            int Round(double x)
            {
                return (int)Math.Round(x);
            }

            #endregion
        }


        #endregion

    }

}


