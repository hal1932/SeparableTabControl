using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Shapes;

namespace SeparableTabControlTest
{
    /// <summary>
    /// グリッド中の TabItem を表示するためのゴースト
    /// </summary>
    class TabItemGhost : Adorner
    {
        public TabItem TargetItem { get; private set; }


        private Rectangle _rect = new Rectangle();
        private Point _topLeft;


        public TabItemGhost(SeparableTabControl tabControl)
            : base(tabControl)
        {
            Visibility = Visibility.Hidden;
        }


        public void Show(TabItem tabItem, Point topLeft)
        {
            Debug.Assert(tabItem != null);
            TargetItem = tabItem;

            _rect.Fill = new VisualBrush(tabItem);
            _rect.Width = tabItem.ActualWidth;
            _rect.Height = tabItem.ActualHeight;

            Visibility = Visibility.Visible;

            _topLeft = topLeft;
            Update();
        }


        public void Move(Point topLeft)
        {
            _topLeft = topLeft;
            Update();
        }


        public void Hide()
        {
            Visibility = Visibility.Hidden;
            _rect.Fill = null;
            TargetItem = null;
        }


        private void Update()
        {
            var layer = Parent as AdornerLayer;
            if (layer != null)
            {
                layer.Update(AdornedElement);
            }
        }


        public override GeneralTransform GetDesiredTransform(GeneralTransform transform)
        {
            var result = new GeneralTransformGroup();
            result.Children.Add(new TranslateTransform(_topLeft.X, _topLeft.Y));
            return result;
        }


        protected override Size ArrangeOverride(Size finalSize)
        {
            _rect.Arrange(new Rect(_rect.DesiredSize));
            return finalSize;
        }


        protected override int VisualChildrenCount
        {
            get
            {
                return 1;
            }
        }


        protected override Visual GetVisualChild(int index)
        {
            return _rect;
        }
    }
}
