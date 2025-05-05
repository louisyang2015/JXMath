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
        Point _center;
        double _radius;
        Brush _brush; // Original color of the circle at construction time

        public string Text { get; private set; }


        public CircleVisual(Point center, double radius, Brush brush, double dataX, double dataY)
        {
            _center = center;
            _radius = radius;
            _brush = brush;

            Text = $"({dataX:g6}, {dataY:g6})";

            ResetColor();
        }

        public void Flag_As_Multiple_Points()
        {
            if (!Text.EndsWith(" ..."))
                Text += " ...";
        }

        /// <summary>
        /// Draws the circle with the specified brush.
        /// </summary>
        public void ReColor(Brush brush)
        {
            using var dc = RenderOpen();
            dc.DrawEllipse(brush, null, _center, _radius, _radius);
        }

        /// <summary>
        /// Draws the circle with its original brush.
        /// </summary>
        public void ResetColor()
        {
            using var dc = RenderOpen();
            dc.DrawEllipse(_brush, null, _center, _radius, _radius);
        }
    }

}
