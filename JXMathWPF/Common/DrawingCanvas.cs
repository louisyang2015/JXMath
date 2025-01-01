using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace JXMathWPF.Common
{
    class DrawingCanvas : Canvas
    {
        List<Visual> _visuals = [];

        protected override int VisualChildrenCount
        {
            get { return _visuals.Count; }
        }

        protected override Visual GetVisualChild(int index)
        {
            return _visuals[index];
        }

        public void AddVisual(Visual visual)
        {
            _visuals.Add(visual);
            AddVisualChild(visual);
            AddLogicalChild(visual);
        }

        public void AddVisuals(IEnumerable<Visual> visuals)
        {
            foreach (var visual in visuals)
                AddVisual(visual);
        }

        public void RemoveVisual(Visual visual)
        {
            _visuals.Remove(visual);
            RemoveVisualChild(visual);
            RemoveLogicalChild(visual);
        }

        public void ClearVisuals()
        {
            foreach (var visual in _visuals)
            {
                RemoveVisualChild(visual);
                RemoveLogicalChild(visual);
            }

            _visuals.Clear();
        }

        public DrawingVisual? GetVisual(Point point)
        {
            HitTestResult hitTestResult = VisualTreeHelper.HitTest(this, point);

            if (hitTestResult != null)
                return hitTestResult.VisualHit as DrawingVisual;

            return null;
        }

    }

}
