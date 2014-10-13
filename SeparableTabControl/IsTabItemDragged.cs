using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace SeparableTabControlTest
{
    /// <summary>
    /// TabItem がドラッグ中かどうかを記録する添付プロパティ
    /// </summary>
    class IsTabItemDragged
    {
        #region ValueProperty
        [AttachedPropertyBrowsableForType(typeof(TabItem))]
        public static bool GetValueProperty(DependencyObject obj)
        {
            return (bool)obj.GetValue(ValueProperty);
        }

        [AttachedPropertyBrowsableForType(typeof(TabItem))]
        public static void SetValueProperty(DependencyObject obj, bool value)
        {
            obj.SetValue(ValueProperty, value);
        }

        // Using a DependencyProperty as the backing store for ValueProperty.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.RegisterAttached("Value", typeof(bool), typeof(IsTabItemDragged), new PropertyMetadata(false));
        #endregion
    }
}
