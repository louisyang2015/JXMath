using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using JXMath;
using JXMathWPF;

using static JXMath.Globals;


namespace WpfTestApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }


        private void Test_Click(object sender, RoutedEventArgs e)
        {
            var x = MakeArray(-10, 0.01, 10);
            Func<double, double> f = x => x * x;
            var y = f.Eval(x);

            var viewer = new XYPlot();
            viewer.AddData(x, y, "y = x^2");

            var x2 = MakeArray(0, 0.01, 10);
            f = x => Math.Pow(x, 0.5);
            y = f.Eval(x2);
            viewer.AddData(x2, y, "y = sqrt(x)", "v");

            f = x => Math.Pow(x, 3);
            y = f.Eval(x);
            viewer.AddData(x, y, "y = x^3", "g");

            viewer.ShowDialog();
        }
    }
}