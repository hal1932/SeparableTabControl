using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;

namespace SeparableTabControlTest
{
    public class SeparableTabControl : TabControl
    {
        /// <summary>
        /// tabItem をウィンドウ化するときに、ウィンドウのタイトルに設定したい文字列を返す
        /// null のときは tabItem.Header.ToString() をタイトル文字列にする
        /// </summary>
        public event OnSetWindowTitle SetWindowTitle;
        public delegate string OnSetWindowTitle(TabItem tabItem);


        private TabItemGhost _tabItemGhost;
        private bool _isItemDragged;


        private class TabItemMetadata
        {
            public int Index;
            public object Header;
        }


        private Dictionary<object, TabItemMetadata> _metadataDic = new Dictionary<object, TabItemMetadata>();


        public SeparableTabControl()
        {
            _tabItemGhost = new TabItemGhost(this);

            // ドラッグ中に半透明になるスタイルを this.ItemContainerStyle にいれておく
            var itemContainerStyle = new Style();
            {
                var trigger = new Trigger()
                {
                    Property = IsTabItemDragged.ValueProperty,
                    Value = true,
                };
                trigger.Setters.Add(new Setter()
                {
                    Property = TabItem.OpacityProperty,
                    Value = 0.5,
                });
                itemContainerStyle.Triggers.Add(trigger);
            }
            ItemContainerStyle = itemContainerStyle;

            Loaded += SeparableTabControl_Loaded;
        }


        private void SeparableTabControl_Loaded(object sender, RoutedEventArgs e)
        {
            // this 以下のビジュアルツリー上でゴースト表示が有効になるようにしておく
            var layer = AdornerLayer.GetAdornerLayer(this);
            Debug.Assert(layer != null);

            layer.Add(_tabItemGhost);
        }


        protected override void OnPreviewMouseMove(MouseEventArgs e)
        {
            // ドラッグ中だったらマウス位置にゴーストを追従させる
            if (!_isItemDragged) return;
            _tabItemGhost.Move(e.GetPosition(null));
        }


        protected override void OnMouseLeave(MouseEventArgs e)
        {
            if (_isItemDragged)
            {
                // ゴーストを非表示に
                var tabItem = _tabItemGhost.TargetItem;
                tabItem.SetValue(IsTabItemDragged.ValueProperty, false);
                _tabItemGhost.Hide();

                // マウス位置にウィンドウを生成
                // ドラッグしてる TabItem の Content をウィンドウ内に表示する
                var pos = PointToScreen(e.GetPosition(null));
                var title = (SetWindowTitle != null) ? SetWindowTitle(tabItem) : tabItem.Header.ToString();
                var content = (FrameworkElement)tabItem.Content;
                var window = new Window()
                {
                    Left = pos.X,
                    Top = pos.Y,
                    Width = content.ActualWidth,
                    Height = content.ActualHeight,
                    Title = title,
                    Content = content,
                    Tag = _metadataDic[content],
                    Owner = Window.GetWindow(this),
                };
                window.PreviewMouseMove += window_PreviewMouseMove;
                window.Closed += window_Closed;
                window.Show();

                // Content を window に移したから TabControl.Items のほうは削除
                Items.Remove(tabItem);

                _isItemDragged = false;
            }
        }


        protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        {
            base.PrepareContainerForItemOverride(element, item);

            var tabItem = (TabItem)element;

            tabItem.PreviewMouseLeftButtonDown += tabItem_PreviewMouseLeftButtonDown;
            tabItem.PreviewMouseLeftButtonUp += tabItem_PreviewMouseLeftButtonUp;

            // OnMouseLeave() でウィンドウ化した TabItem を TabControl の中に
            // 復帰させるときに使うデータを保管しておく
            TabItemMetadata metadata;
            if (!_metadataDic.TryGetValue(tabItem.Content, out metadata))
            {
                _metadataDic[tabItem.Content] = new TabItemMetadata()
                {
                    Index = Items.IndexOf(tabItem),
                    Header = tabItem.Header,
                };
            }
        }


        protected override void ClearContainerForItemOverride(DependencyObject element, object item)
        {
            var tabItem = (TabItem)element;
            tabItem.PreviewMouseLeftButtonUp -= tabItem_PreviewMouseLeftButtonDown;
            tabItem.PreviewMouseLeftButtonUp -= tabItem_PreviewMouseLeftButtonUp;

            base.ClearContainerForItemOverride(element, item);
        }


        private void tabItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (IsEventFromHeader(e))
            {
                // 「ドラッグ中」フラグを立てる
                var tabItem = (TabItem)sender;
                tabItem.SetValue(IsTabItemDragged.ValueProperty, true);

                // ゴースト表示
                _tabItemGhost.Show(tabItem, Mouse.GetPosition(null));

                _isItemDragged = true;
            }
        }


        private void tabItem_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // 「ドラッグ中」フラグを折る
            var tabItem = (TabItem)sender;
            tabItem.SetValue(IsTabItemDragged.ValueProperty, false);

            // ゴースト非表示
            _tabItemGhost.Hide();

            _isItemDragged = false;
        }


        private void window_Closed(object sender, EventArgs e)
        {
            // ウィンドウが閉じたら、TabControl の中に復帰させる
            var window = (Window)sender;
            var content = window.Content;

            var metadata = _metadataDic[content];

            var tabItem = new TabItem()
            {
                Header = metadata.Header,
                Content = content,
            };

            var index = Math.Min(metadata.Index, Items.Count);
            Items.Insert(index, tabItem);
            SelectedIndex = index;
        }


        private void window_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            // ドラッグしたままウィンドウを動かせるように
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                ((Window)sender).DragMove();
            }
        }


        private bool IsEventFromHeader(RoutedEventArgs e)
        {
            // TabControl のビジュアルツリー
            //
            // TabControl
            //  ├─ TabPanel
            //  │   └─ TabItem      // ← ここが Header 部分
            //  └─ ContentPresenter // ← ここが Content 部分
            //
            // Header 部分でイベントがおきたときは
            // e.Source.GetType() == typeof(TabItem) になる
            //
            // Content 部分でイベントが起きたときは
            // e.Source.GetType() は Content に応じて変わる
            // たとえば
            // <TabItem><Grid/></TabItem> のときは e.Source.GetType() == typeof(Grid)
            //
            return (e.Source is TabItem);
        }
    }
}   
