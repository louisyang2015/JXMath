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

        // Highlighting
        Brush _highlight_brush = Brushes.Orange;
        CircleVisual? _highlighted_circle = null;
        

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


        void RemoveOldHighlight()
        {
            if (_highlighted_circle != null)
            {
                _highlighted_circle.ResetColor();
                _highlighted_circle = null;
            }
        }


        #region Event Handlers

        private void DataSets_lb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_disable_gui) return;

            _view_port.Reset(_data, DataSets_lb);
            RedrawGraph();
        }

        /// <summary>
        /// When user clicked on one of the menu items associated with data scaling.
        /// </summary>
        private void Scale_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_disable_gui) return;

                var menu_item = (MenuItem)sender;
                if (menu_item.IsChecked) return;

                // Code arrive here if action is needed

                // Update the menu item check status
                // First set the related menu items to unchecked
                List<MenuItem> x_items = [XNormal_menuItem, XLog10_menuItem];
                List<MenuItem> y_items = [YNormal_menuItem, YLog10_menuItem];

                if (x_items.Contains(menu_item))
                {
                    foreach (var item in x_items)
                        item.IsChecked = false;
                }
                else if (y_items.Contains(menu_item))
                {
                    foreach (var item in y_items)
                        item.IsChecked = false;
                }

                menu_item.IsChecked = true;

                // Determine the necessary transforms
                var transform_lookup = new Dictionary<MenuItem, string>() {
                    { XNormal_menuItem, "" },
                    { XLog10_menuItem, "log10" },
                    { YNormal_menuItem, "" },
                    { YLog10_menuItem, "log10" }
                };

                string x_transform = "", y_transform = "";

                foreach (var item in x_items)
                {
                    if (item.IsChecked)
                    {
                        x_transform = transform_lookup[item];
                        break;
                    }
                }

                foreach (var item in y_items)
                {
                    if (item.IsChecked)
                    {
                        y_transform = transform_lookup[item];
                        break;
                    }
                }

                // Apply the transform
                foreach (var ds in _data)
                {
                    ds.ApplyTransform(x_transform, y_transform);
                }

                _view_port.Reset(_data, DataSets_lb);
                RedrawGraph();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error");
            }
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

        private void Window_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0)
            {
                _view_port.ZoomX *= 1.1;
                _view_port.ZoomY *= 1.1;
            }
            else
            {
                if (_view_port.ZoomX == 1 && _view_port.ZoomY == 1)
                    return; // Don't zoom out more than 1x (100%)

                _view_port.ZoomX /= 1.1;
                _view_port.ZoomY /= 1.1;
            }

            ZoomX_tb.Text = _view_port.ZoomX.ToString("g4");
            ZoomY_tb.Text = _view_port.ZoomY.ToString("g4");

            RedrawGraph();
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_disable_gui) return;

            RedrawGraph();
        }

        private void XYGraph_canvas_KeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = true;

            if (e.Key == Key.Left)
                _view_port.Move("left");
            else if (e.Key == Key.Right)
                _view_port.Move("right");
            else if (e.Key == Key.Up)
                _view_port.Move("up");
            else if (e.Key == Key.Down)
                _view_port.Move("down");
            else
                // Don't handle other keys
                return;

            RedrawGraph();
        }

        private void XYGraph_canvas_MouseMove(object sender, MouseEventArgs e)
        {
            Point p = e.GetPosition(XYGraph_canvas);

            if (HandleMouseDrag(e, p)) return;

            // Handle regular mouse move

            var visual = XYGraph_canvas.GetVisual(p);

            if (visual is CircleVisual circle)
            {
                if (circle != _highlighted_circle)
                {
                    RemoveOldHighlight();
                    circle.ReColor(_highlight_brush);
                    _highlighted_circle = circle;
                    Status_tb.Text = circle.Text;
                }
            }
            else
            {
                RemoveOldHighlight();
                Status_tb.Text = "";
            }
        }

        private void XYGraph_canvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            XYGraph_canvas.Focus();
        }
                
        private void Zoom_tb_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                // Check user input
                if (!double.TryParse(ZoomX_tb.Text, out _))
                {
                    MessageBox.Show($"Invalid zoom X level '{ZoomX_tb.Text}'.");
                    return;
                }

                if (!double.TryParse(ZoomY_tb.Text, out _))
                {
                    MessageBox.Show($"Invalid zoom Y level '{ZoomY_tb.Text}'.");
                    return;
                }

                RedrawGraph();
            }
        }

        #region Mouse Dragging

        // To handle mouse dragging
        Point? _last_mouse_down = null;
        DateTime _last_mouse_down_time;

        // To debounce the mouse events: 
        MouseButtonState _mouse_state = MouseButtonState.Released;
        int _mouse_filter = 0; 

        /// <summary>
        /// Helper for "XYGraph_canvas_MouseMove()". Returns true if any mouse
        /// drag event has been handled.
        /// </summary>
        /// <param name="p">Mouse position relative to "XYGraph_canvas".</param>
        bool HandleMouseDrag(MouseEventArgs e, Point p)
        {
            // Filter the mouse state changes
            if (_mouse_state != e.LeftButton)
                _mouse_filter++;
            else
                _mouse_filter = 0;

            if (_mouse_filter >= 5)
                _mouse_state = e.LeftButton;

            // Handle mouse dragging
            if (_mouse_state == MouseButtonState.Pressed)
            {
                if (_last_mouse_down == null)
                {
                    // First time mouse down
                    _last_mouse_down = p;
                    _last_mouse_down_time = DateTime.Now;
                    return true;
                }

                // Code arrive here if the mouse is being dragged

                // Skip handling if event occurred too frequently avoid calling
                // RedrawGraph() too often
                TimeSpan time_diff = DateTime.Now - _last_mouse_down_time;
                if (time_diff.TotalMilliseconds < 100)
                    return true;

                // Skip handling if the mouse moved too little
                Point delta = new Point(p.X - _last_mouse_down.Value.X, p.Y - _last_mouse_down.Value.Y);

                if (Abs(delta.X) < 24 && Abs(delta.Y) < 24)
                    return true;

                double dx = (delta.X / XYGraph_canvas.ActualWidth) * (_view_port.XMax - _view_port.XMin);
                double dy = (delta.Y / XYGraph_canvas.ActualHeight) * (_view_port.YMax - _view_port.YMin);

                dx = -dx; // Invert X direction because "dx" positive, which means dragging a point
                // to the right, actually means the graph is moving to the left.
                // The "dy" is already inverted. "dy" positive is the mouse moving down, which
                // means dragging a point down, which means moving the graph up.

                _view_port.Move(dx, dy);

                RedrawGraph();

                _last_mouse_down = p;
                _last_mouse_down_time = DateTime.Now;

                return true;
            }
            else
            {
                // Cancel any mouse dragging if the left button is released
                _last_mouse_down = null;
                return false;
            }
        }

        #endregion


        #endregion


        #region Inner Classes

        class GraphSettings
        {
            public double Width = 200; // Will be set to Canvas actual
            public double Height = 100; // Will be set to Canvas height

            public double CircleRadius = 8+2;
        }


        /// <summary>
        /// A pair of (double[] x, double[] y) data points.
        /// </summary>
        class DataSet
        {
            // The pair (x, y) is sorted by x by the end of construction
            double[] _x, _y;

            // The "_x" and "_y" arrays are the unmodified data arrays
            // They can be transformed - such as log scale
            // The transformed data, which is used for plotting, are the "X" and "Y"
            public double[] X { get; private set; }
            public double[] Y { get; private set; }

            public Brush Brush { get; private set; }

            // After construction, the "X" is sorted
            public double XMin { get => X[0]; }
            public double XMax { get => X[^1]; }
            public double YMin { get; private set; }
            public double YMax { get; private set; }

            /// <summary>
            /// Records the current transformed applied to "_x" and "_y"
            /// </summary>
            string _x_transform = "";
            string _y_transform = "";

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
                _x = x;
                _y = y;

                if (_color_shortcut.ContainsKey(color.ToLower()))
                    color = _color_shortcut[color];

                try
                {
                    var c = (Color)ColorConverter.ConvertFromString(color);
                    Brush = new SolidColorBrush(c);
                }
                catch
                {
                    throw new Exception($"Invalid color: '{color}'.");
                }

                Check_and_Duplicate_Data();
                SortByX(_x, _y);

                // By default there's no transform
                X = _x;
                Y = _y;
                YMin = Y.Min();
                YMax = Y.Max();
            }


            /// <summary>
            /// Checks the "_x" and "_y" data arrays to be the same length and sorted by _x. 
            /// Duplicate the data so there's no chance of corrupting the data from
            /// the caller.
            /// </summary>
            void Check_and_Duplicate_Data()
            {
                bool duplicated = false;

                // Check that _x and _y are the same length
                if (_x.Length != _y.Length)
                {
                    // Fix the length by allocating new arrays
                    int new_length = Min(_x.Length, _y.Length);
                    (_x, _y) = Duplicate(_x, _y, new_length);
                    duplicated = true;
                }

                if (!duplicated)
                {
                    (_x, _y) = Duplicate(_x, _y);
                }
            }


            /// <summary>
            /// Apply a transform to the "_x" and "_y". The result is in 
            /// "X" and "Y" properties, which is what gets plotted.
            /// </summary>
            /// <param name="xTransform">"", "log10"</param>
            /// <param name="yTransform">"", "log10"</param>
            public void ApplyTransform(string xTransform, string yTransform)
            {
                if (_x_transform == xTransform && _y_transform == yTransform)
                    return;

                // Code arrive here if action is needed

                // Special case, if there is no transform applied
                if (xTransform == "" && yTransform == "")
                {
                    X = _x;
                    Y = _y;
                    YMin = Y.Min();
                    YMax = Y.Max();
                    _x_transform = "";
                    _y_transform = "";
                    return;
                }

                // Initially, reallocate (X, Y) to be the same size as (_x, _y)
                X = new double[_x.Length];
                Y = new double[_y.Length];

                // Apply the transform
                // Not all data can be transformed, so track how much of (X, Y)
                // gets populated.
                int k = 0; // The index for X and Y

                for (int i = 0; i < _x.Length; i++)
                {
                    double? x = Transform(xTransform, _x[i]);
                    double? y = Transform(yTransform, _y[i]);

                    if (x == null || y == null)
                        continue;
                    else
                    {
                        X[k] = x.Value;
                        Y[k] = y.Value;
                        k++;
                    }
                }

                // Re-allocate the (X, Y) if needed
                if (k < X.Length)
                {
                    (X, Y) = Duplicate(X, Y, k);
                }

                // At this point, X and Y are transformed versions of _x and _y.
                SortByX(_x, _y);
                YMin = Y.Min();
                YMax = Y.Max();

                _x_transform = xTransform;
                _y_transform = yTransform;
            }


            #region Helpers

            /// <summary>
            /// Duplicate the given arrays (x, y) to be new arrays, same content, 
            /// but possibly a shorter length.
            /// </summary>
            (double[] x, double[] y) Duplicate(double[] x, double[] y, int length = -1)
            {
                if (length < 0)
                    length = x.Length;

                double[] new_x = new double[length];
                double[] new_y = new double[length];

                for (int i = 0; i < length; i++)
                {
                    new_x[i] = x[i];
                    new_y[i] = y[i];
                }

                return (new_x, new_y);
            }


            /// <summary>
            /// Sort the given arrays (x, y) by x.
            /// </summary>
            void SortByX(double[] x, double[] y)
            {
                // Sorting by x makes filtering more efficient
                // Check that x is sorted
                bool sorted = true;

                for (int i = 0; i < x.Length - 1; i++)
                {
                    if (x[i] > x[i + 1])
                    {
                        sorted = false;
                        break;
                    }
                }

                if (!sorted)
                    Array.Sort(x, y);
            }


            /// <summary>
            /// Return the transformed version of "value". Returns null
            /// if the transform is not valid.
            /// </summary>
            /// <param name="transform">"", "log10"</param>
            double? Transform(string transform, double value)
            {
                if (transform == "")
                    return value;
                else if (transform == "log10")
                {
                    if (value <= 0)
                        return null;

                    return Log10(value);
                }

                throw new Exception($"Invalid transform: '{transform}'.");
            }

            #endregion
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
                    _zoomX = value;
                    if (_zoomX < 1)
                        _zoomX = 1;

                    // Update XMin and XMax
                    double dx = (_data_x_max - _data_x_min) / _zoomX;
                    XMin = CenterX - dx / 2.0;
                    XMax = CenterX + dx / 2.0;
                }
            }

            double _zoomY = 1;
            public double ZoomY
            {
                get => _zoomY;
                set
                {
                    _zoomY = value;
                    if (_zoomY < 1)
                        _zoomY = 1;

                    // Update YMin and YMax
                    double dy = (_data_y_max - _data_y_min) / _zoomY;
                    YMin = CenterY - dy / 2.0;
                    YMax = CenterY + dy / 2.0;
                }
            }

            // The ultimate min and max values. It's actually a bit smaller / larger 
            // than the data min / max values, due to the margin added.
            double _data_x_min, _data_x_max;
            double _data_y_min, _data_y_max;

            // The current min and max values:
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

                // _data_x_min, _data_x_max, _data_y_min, _data_y_max
                _data_x_min = XMin;
                _data_x_max = XMax;
                _data_y_min = YMin;
                _data_y_max = YMax;

                // CenterX, CenterY
                CenterX = (XMin + XMax) / 2.0;
                CenterY = (YMin + YMax) / 2.0;
            }

            public void Move(string direction)
            {
                double zoom_factor = 1.0 / ZoomX;
                double dx = (XMax - XMin) * 0.05;

                zoom_factor = 1.0 / ZoomY;
                double dy = (YMax - YMin) * 0.05;

                if (direction == "up")
                    CenterY += dy;
                else if (direction == "down")
                    CenterY -= dy;
                else if (direction == "left")
                    CenterX -= dx;
                else if (direction == "right")
                    CenterX += dx;
            }

            public void Move(double dx, double dy)
            {
                CenterX += dx;
                CenterY += dy;
                XMin += dx;
                XMax += dx;
                YMin += dy;
                YMax += dy;
            }
        }


        /// <summary>
        /// Translate data points to screen coordinates.
        /// </summary>
        class PlotPipeLine
        {
            // Use a dictionary to map multiple data points to a single visual
            Dictionary<(int, int), CircleVisual> _visuals = [];

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

                // The "_visuals" is a dictionary to group points that are close to
                // each other. This is to reduce the number of visuals to be drawn.
                // Compute the step size for the "_visuals" grid.
                // This step size is chosen so that at most the points will have a minor 
                // overlap.
                double gap = _settings.CircleRadius * 2 * 0.9; // gap between two points
                double step = gap / 2;
                // Imagine three points, spaced out by "step"
                // First two points will be grouped together, drawn as one point.
                // The third point will be around "gap" away from the first point, 
                // and thus of the three points, the user sees two points "gap" apart.

                // Get the number of rows and columns of visuals
                int visual_columns = (int)Floor(_settings.Width / step) + 1;
                int visual_rows = (int)Floor(_settings.Height / step) + 1;

                // The conversion ratio from the data (x, y) to the visual coordinate
                // data_x / (data x max - data x min) * (canvas width) = screen x coordinate
                double x_multiplier = _settings.Width / (_view.XMax - _view.XMin);
                double y_multiplier = _settings.Height / (_view.YMax - _view.YMin);

                foreach (var d in data)
                {
                    // Determine the range of x-indices that are within the view port
                    (int x_left, int x_right) = Determine_X_Index_Range(d.X);

                    for (int x = x_left; x <= x_right; x++)
                    {
                        // Filter out Y values that are outside the view port
                        if (d.Y[x] < _view.YMin || d.Y[x] > _view.YMax)
                            continue;

                        // Code arrive here if the data point is within the view port
                        // For each data point in the view port, create a visual object

                        int visual_x = Round((d.X[x] - _view.XMin) * x_multiplier / step);
                        int visual_y = Round((d.Y[x] - _view.YMin) * y_multiplier / step);

                        if (visual_x >= 0 && visual_x < visual_columns
                            && visual_y >= 0 && visual_y < visual_rows)
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
                    data.Brush, data_x, data_y);
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


