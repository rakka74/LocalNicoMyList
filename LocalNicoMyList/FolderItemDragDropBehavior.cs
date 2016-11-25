using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interactivity;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;

namespace LocalNicoMyList
{
    class FolderItemDragDropBehavior : Behavior<FrameworkElement>
    {
        #region ■■■■■ 依存関係プロパティ

        public static readonly DependencyProperty IsDragEnabledProperty =
            DependencyProperty.Register("IsDragEnabled", typeof(bool),
            typeof(FolderItemDragDropBehavior), new UIPropertyMetadata(true));

        public bool IsDragEnabled
        {
            get { return (bool)GetValue(IsDragEnabledProperty); }
            set {
                SetValue(IsDragEnabledProperty, value);
            }
        }

        #endregion

        protected override void OnAttached()
        {
            this.AssociatedObject.PreviewMouseLeftButtonDown += previewMouseLeftButtonDown;
            this.AssociatedObject.PreviewMouseMove += previewMouseMove;
            this.AssociatedObject.QueryContinueDrag += queryContinueDrag;
            //this.AssociatedObject.DragEnter += dragEnter;
            //this.AssociatedObject.DragOver += dragOver;
            this.AssociatedObject.Drop += drop;
            this.AssociatedObject.PreviewDragEnter += previewDragEnter;
            this.AssociatedObject.PreviewDragLeave += previewDragLeave;

        }

        protected override void OnDetaching()
        {
            this.AssociatedObject.PreviewMouseLeftButtonDown -= previewMouseLeftButtonDown;
            this.AssociatedObject.PreviewMouseMove -= previewMouseMove;
            this.AssociatedObject.QueryContinueDrag -= queryContinueDrag;
            //this.AssociatedObject.DragEnter -= dragEnter;
            //this.AssociatedObject.DragOver -= dragOver;
            this.AssociatedObject.Drop -= drop;
            this.AssociatedObject.PreviewDragEnter -= previewDragEnter;
            this.AssociatedObject.PreviewDragLeave -= previewDragLeave;
        }

        Point? _mouseDownPt = null;
        Point _mouseOffsetFromItem;

        private void previewMouseLeftButtonDown(object sender_, MouseButtonEventArgs e)
        {
            var sender = sender_ as ItemsControl;
            var lvi = sender.ContainerFromElement(e.OriginalSource as DependencyObject) as ListViewItem;
            if (null != lvi && this.IsDragEnabled)
            {
                // マウスダウン時の座標を取得
                _mouseDownPt = sender.PointToScreen(e.GetPosition(sender));

                _mouseOffsetFromItem = lvi.PointFromScreen(_mouseDownPt.Value);
            }
        }

        DragAdorner _dragContentAdorner;

        private void previewMouseMove(object sender_, MouseEventArgs e)
        {
            var sender = sender_ as ItemsControl;
            if (e.LeftButton != MouseButtonState.Pressed || !_mouseDownPt.HasValue)
            {
                return;
            }
            var point = sender.PointToScreen(e.GetPosition(sender));
            if (this.checkDistance(point, _mouseDownPt.Value))
            {
                var lvi = ((ItemsControl)sender).ContainerFromElement(e.OriginalSource as DependencyObject) as ListViewItem;
                _dragContentAdorner = new DragAdorner(sender, lvi, 0.7, _mouseOffsetFromItem);

                FolderItem folderItem = lvi?.DataContext as FolderItem;
                DragDrop.DoDragDrop(lvi, folderItem, DragDropEffects.Move);

                _dragContentAdorner.Dispose();
                _dragContentAdorner = null;
                _insertionMarkAdorner?.Dispose();
                _insertionMarkAdorner = null;

                _mouseDownPt = null;

                e.Handled = true;
            }
        }

        private bool checkDistance(Point x, Point y)
        {
            return Math.Abs(x.X - y.X) >= SystemParameters.MinimumHorizontalDragDistance ||
                Math.Abs(x.Y - y.Y) >= SystemParameters.MinimumVerticalDragDistance;
        }

        private void queryContinueDrag(object sender_, QueryContinueDragEventArgs e)
        {
            var sender = sender_ as ItemsControl;
            if (_dragContentAdorner != null)
            {
                var p = CursorInfo.GetNowPosition(sender);
                var loc = sender.PointFromScreen(sender.PointToScreen(new Point(0, 0)));
                _dragContentAdorner.LeftOffset = p.X - loc.X;
                _dragContentAdorner.TopOffset = p.Y - loc.Y;
            }
        }

        //private void dragEnter(object sender, DragEventArgs e)
        //{
        //}

        //private void dragOver(object sender, DragEventArgs e)
        //{
        //}

        private void drop(object sender, DragEventArgs e)
        {
            var lvi = ((ItemsControl)sender).ContainerFromElement(e.OriginalSource as DependencyObject) as ListViewItem;
            if (null == lvi)
                return;

            if (e.Data.GetDataPresent(typeof(FolderItem)))
            {
                var itemsSource = ((ItemsControl)sender).ItemsSource as ObservableCollection<FolderItem>;

                var targetFolderItem = lvi.DataContext as FolderItem;
                int newIndex = itemsSource.IndexOf(targetFolderItem);

                var draggedFolderItem = e.Data.GetData(typeof(FolderItem)) as FolderItem;
                int oldIndex = itemsSource.IndexOf(draggedFolderItem);

                if (oldIndex != newIndex)
                {
                    itemsSource.Move(oldIndex, newIndex);
                    // DBに保存
                    MainWindow.instance.folderReordered();
                }
            }
        }

        InsertionMarkAdorner _insertionMarkAdorner;

        private void previewDragEnter(object sender, DragEventArgs e)
        {
            var lvi = ((ItemsControl)sender).ContainerFromElement(e.OriginalSource as DependencyObject) as ListViewItem;
            if (null == lvi)
                return;

            if (e.Data.GetDataPresent(typeof(FolderItem)))
            {

                var draggedFolderItem = e.Data.GetData(typeof(FolderItem)) as FolderItem;
                var tagetFolderItem = lvi.DataContext as FolderItem;

                var itemsSource = ((ItemsControl)sender).ItemsSource as ObservableCollection<FolderItem>;

                int draggedFolderIdx = itemsSource.IndexOf(draggedFolderItem);
                int targetFolderIdx = itemsSource.IndexOf(tagetFolderItem);

                if (draggedFolderIdx == targetFolderIdx)
                    return;
                _insertionMarkAdorner = new InsertionMarkAdorner(lvi, draggedFolderIdx < targetFolderIdx ? InsertionMarkAdorner.Edge.Bottom : InsertionMarkAdorner.Edge.Top);
            }
        }

        private void previewDragLeave(object sender, DragEventArgs e)
        {
            _insertionMarkAdorner?.Dispose();
            _insertionMarkAdorner = null;
        }
    }

    class AdornerBase : Adorner, IDisposable
    {
        private AdornerLayer _adornerLayer;

        public AdornerBase(UIElement adornedElement) : base(adornedElement)
        {
            _adornerLayer = AdornerLayer.GetAdornerLayer(adornedElement);
            _adornerLayer.Add(this);

            this.IsHitTestVisible = false;
        }

        public void Dispose()
        {
            _adornerLayer.Remove(this);
        }
    }


    class InsertionMarkAdorner : AdornerBase
    {
        public enum Edge
        {
            Top,
            Bottom
        }

        Edge _edge;

        public InsertionMarkAdorner(UIElement adornedElement, Edge edge) : base(adornedElement)
        {
            _edge = edge;
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            if (_edge == Edge.Top)
                drawingContext.DrawLine(new Pen(Brushes.DodgerBlue, 5), new Point(0, 0), new Point(this.ActualWidth, 0));
            else
                drawingContext.DrawLine(new Pen(Brushes.DodgerBlue, 5), new Point(0, this.ActualHeight), new Point(this.ActualWidth, this.ActualHeight));
        }
    }

    class DragAdorner : AdornerBase
    {
        protected UIElement _child;
        protected double XCenter;
        protected double YCenter;

        public DragAdorner(UIElement owner) : base(owner) { }

        public DragAdorner(UIElement owner, UIElement adornElement, double opacity, Point dragPos)
            : base(owner)
        {
            var _brush = new VisualBrush(adornElement) { Opacity = opacity };
            var b = VisualTreeHelper.GetDescendantBounds(adornElement);
            var r = new Rectangle() { Width = b.Width, Height = b.Height };

            XCenter = dragPos.X;// r.Width / 2;
            YCenter = dragPos.Y;// r.Height / 2;

            r.Fill = _brush;
            _child = r;
        }


        private double _leftOffset;
        public double LeftOffset
        {
            get { return _leftOffset; }
            set
            {
                _leftOffset = value - XCenter;
                UpdatePosition();
            }
        }

        private double _topOffset;
        public double TopOffset
        {
            get { return _topOffset; }
            set
            {
                _topOffset = value - YCenter;
                UpdatePosition();
            }
        }

        private void UpdatePosition()
        {
            var adorner = this.Parent as AdornerLayer;
            if (adorner != null)
            {
                adorner.Update(this.AdornedElement);
            }
        }

        protected override Visual GetVisualChild(int index)
        {
            return _child;
        }

        protected override int VisualChildrenCount
        {
            get { return 1; }
        }

        protected override Size MeasureOverride(Size finalSize)
        {
            _child.Measure(finalSize);
            return _child.DesiredSize;
        }
        protected override Size ArrangeOverride(Size finalSize)
        {

            _child.Arrange(new Rect(_child.DesiredSize));
            return finalSize;
        }

        public override GeneralTransform GetDesiredTransform(GeneralTransform transform)
        {
            var result = new GeneralTransformGroup();
            result.Children.Add(base.GetDesiredTransform(transform));
            result.Children.Add(new TranslateTransform(_leftOffset, _topOffset));
            return result;
        }
    }

    public static class CursorInfo
    {
        [DllImport("user32.dll")]
        private static extern void GetCursorPos(out POINT pt);

        [DllImport("user32.dll")]
        private static extern int ScreenToClient(IntPtr hwnd, ref POINT pt);

        private struct POINT
        {
            public UInt32 X;
            public UInt32 Y;
        }

        public static Point GetNowPosition(Visual v)
        {
            POINT p;
            GetCursorPos(out p);

            var source = HwndSource.FromVisual(v) as HwndSource;
            var hwnd = source.Handle;

            ScreenToClient(hwnd, ref p);
            return new Point(p.X, p.Y);
        }
    }
}
