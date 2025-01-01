using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace JXMathWPF.Common
{
    class CircleVisual : DrawingVisual
    {
        public string Name { get; private set; }

        public CircleVisual(Point center, double radius, Color color, double dataX, double dataY)
        {
            Name = $"({dataX}, {dataY})";

            using (DrawingContext dc = RenderOpen())
            {
                dc.DrawEllipse(new SolidColorBrush(color), null, center, radius, radius);
            }
        }

        public void Flag_As_Multiple_Points()
        {
            Name += " ...";
        }
    }

}
