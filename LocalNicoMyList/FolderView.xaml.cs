using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace LocalNicoMyList
{
    /// <summary>
    /// FolderView.xaml の相互作用ロジック
    /// </summary>
    public partial class FolderView : UserControl
    {
        public ObservableCollection<FolderItem> _folderListItemSource;
        public FolderItem _selectedFolderItem;

        bool _preventDragFolder;

        public FolderView()
        {
            InitializeComponent();

            _folderListItemSource = new ObservableCollection<FolderItem>();
            _folderListView.DataContext = _folderListItemSource;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
        }

        #region ■■■■■ フォルダ一覧 ListView

        private void _folderListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FolderItem item = e.AddedItems[0] as FolderItem;
            _selectedFolderItem = item;

            MainWindow.instance.folderListView_SelectionChanged(item);
        }

        private void folderListView_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // アイテムがない場所をクリックされてもフォーカス移動するようにする。
            _folderListView.Focus();
        }
        private void folderListView_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            // アイテムがない場所をクリックされてもフォーカス移動するようにする。
            _folderListView.Focus();
        }

        Point? _mouseDownPt = null;
        Point _mouseOffsetFromItem;

        private void folderListView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var lvi = ((ItemsControl)sender).ContainerFromElement(e.OriginalSource as DependencyObject) as ListViewItem;
            if (null != lvi && !_preventDragFolder)
            {
                // マウスダウン時の座標を取得
                _mouseDownPt = this.PointToScreen(e.GetPosition(this));

                _mouseOffsetFromItem = lvi.PointFromScreen(_mouseDownPt.Value);
            }
        }

        private void folderListView_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            _folderListView.Focus();
            // ハンドルすることで右クリックでアイテムが選択されなくなる
            e.Handled = true;
        }

        DragAdorner _dragContentAdorner;

        private void folderListView_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed || !_mouseDownPt.HasValue)
            {
                return;
            }
            var point = this.PointToScreen(e.GetPosition(this));
            if (this.checkDistance(point, _mouseDownPt.Value))
            {
                var lvi = ((ItemsControl)sender).ContainerFromElement(e.OriginalSource as DependencyObject) as ListViewItem;
                _dragContentAdorner = new DragAdorner(_folderListView, lvi, 0.7, _mouseOffsetFromItem);

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

        private void folderListView_QueryContinueDrag(object sender, QueryContinueDragEventArgs e)
        {
            if (_dragContentAdorner != null)
            {
                var p = CursorInfo.GetNowPosition(this);
                var loc = this.PointFromScreen(_folderListView.PointToScreen(new Point(0, 0)));
                _dragContentAdorner.LeftOffset = p.X - loc.X;
                _dragContentAdorner.TopOffset = p.Y - loc.Y;
            }
        }

        private void folderListView_DragEnter(object sender, DragEventArgs e)
        {
            var lvi = ((ItemsControl)sender).ContainerFromElement(e.OriginalSource as DependencyObject) as ListViewItem;

            if (null == lvi)
            {
                e.Effects = DragDropEffects.None;
            }
            else {
                if (e.Data.GetDataPresent(typeof(FolderItem)))
                {
                }
                else if (e.Data.GetDataPresent(typeof(MyListItem)))
                {
                    e.Effects = DragDropEffects.None;

                    var folderItem = lvi.DataContext as FolderItem;

                    if (folderItem.id != _selectedFolderItem.id)
                    {
                        if (0 != (e.KeyStates & DragDropKeyStates.ControlKey))
                            e.Effects = DragDropEffects.Copy;
                        else
                            e.Effects = DragDropEffects.Move;
                        // ドロップ先のフォルダのListViewItemの色を変更
                        folderItem.isMyListItemDropTarget = true;
                    }
                }
            }
            e.Handled = true;
        }

        private void folderListView_DragOver(object sender, DragEventArgs e)
        {
            folderListView_DragEnter(sender, e);
        }

        private void folderListView_DragLeave(object sender, DragEventArgs e)
        {
            var lvi = ((ItemsControl)sender).ContainerFromElement(e.OriginalSource as DependencyObject) as ListViewItem;
            if (null != lvi)
            {
                var folderItem = lvi.DataContext as FolderItem;
                folderItem.isMyListItemDropTarget = false;
            }
        }

        private void folderListView_Drop(object sender, DragEventArgs e)
        {
            var lvi = ((ItemsControl)sender).ContainerFromElement(e.OriginalSource as DependencyObject) as ListViewItem;
            if (null == lvi)
                return;

            if (e.Data.GetDataPresent(typeof(FolderItem)))
            {
                var targetFolderItem = lvi.DataContext as FolderItem;
                int newIndex = _folderListItemSource.IndexOf(targetFolderItem);

                var draggedFolderItem = e.Data.GetData(typeof(FolderItem)) as FolderItem;
                int oldIndex = _folderListItemSource.IndexOf(draggedFolderItem);

                if (oldIndex != newIndex)
                {
                    _folderListItemSource.Move(oldIndex, newIndex);
                    // DBに保存
                    MainWindow.instance.folderReordered();
                }
            }
            else if (e.Data.GetDataPresent(typeof(MyListItem)))
            {
                var folderItem = lvi.DataContext as FolderItem;
                if (0 != (e.KeyStates & DragDropKeyStates.ControlKey))
                    MainWindow.instance.copySelectedMyListItem(folderItem);
                else
                    MainWindow.instance.moveSelectedMyListItem(folderItem);

                folderItem.isMyListItemDropTarget = false;
            }
        }

        InsertionMarkAdorner _insertionMarkAdorner;

        private void folderListView_PreviewDragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(FolderItem)))
            {
                var lvi = _folderListView.ContainerFromElement(e.OriginalSource as DependencyObject) as ListViewItem;

                var draggedFolderItem = e.Data.GetData(typeof(FolderItem)) as FolderItem;
                var tagetFolderItem = lvi.DataContext as FolderItem;

                int draggedFolderIdx = _folderListItemSource.IndexOf(draggedFolderItem);
                int targetFolderIdx = _folderListItemSource.IndexOf(tagetFolderItem);

                if (draggedFolderIdx == targetFolderIdx)
                    return;
                _insertionMarkAdorner = new InsertionMarkAdorner(lvi, draggedFolderIdx < targetFolderIdx ? InsertionMarkAdorner.Edge.Bottom : InsertionMarkAdorner.Edge.Top);
            }
        }

        private void folderListView_PreviewDragLeave(object sender, DragEventArgs e)
        {
            _insertionMarkAdorner?.Dispose();
            _insertionMarkAdorner = null;
        }

        #endregion

        #region ■■■■■ コンテキストメニュー

        FolderItem _cotextMenuFolderItem;

        private FolderItem getAnchorFolderItem(ContextMenu menu)
        {
            ListViewItem listViewItem = menu.PlacementTarget as ListViewItem;
            FolderItem folderItem = listViewItem?.Content as FolderItem;
            return folderItem;
        }

        private void ContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            if (null != _cotextMenuFolderItem)
                _cotextMenuFolderItem.showedContextMenu = false;

            ContextMenu menu = sender as ContextMenu;
            var folderItem = this.getAnchorFolderItem(menu);
            folderItem.showedContextMenu = true;
            _cotextMenuFolderItem = folderItem;
        }

        private void ContextMenu_Closed(object sender, RoutedEventArgs e)
        {
            ContextMenu menu = sender as ContextMenu;
            var folderItem = this.getAnchorFolderItem(menu);
            folderItem.showedContextMenu = false;
            _cotextMenuFolderItem = null;
        }

        private void addFolder_Click(object sender, RoutedEventArgs e)
        {
            string baseName = "新しいフォルダー";
            string name;
            int num = 1;
            while (true)
            {
                name = (1 == num) ? baseName : string.Format("{0} ({1})", baseName, num);
                if (null == _folderListItemSource.FirstOrDefault((_) => { return _.name.Equals(name); }))
                    break;
                ++num;
            }

            long folderId = -1;// _dbAccessor.addFolder(name, folderListItemSource.Count);
            FolderItem folderItem = new FolderItem(folderId, name);
            _folderListItemSource.Add(folderItem);

            int index = _folderListItemSource.IndexOf(folderItem);
            this.Dispatcher.BeginInvoke((Action)(async () =>
            {
                while (true)
                {
                    var viewItem = _folderListView.ItemContainerGenerator.ContainerFromIndex(index) as ListViewItem;
                    if (null != viewItem) break;
                    await Task.Delay(100);
                }
                startEditFolderListItem(folderItem);
            }));
        }

        private void removeFolder_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            var menu = menuItem.Parent as ContextMenu;

            var folderItem = this.getAnchorFolderItem(menu);

            if (_folderListItemSource.Count == 1)
                return;

            folderItem.isContextMenuCommandTarget = true;

            var dialog = new TaskDialog();
            dialog.Caption = Properties.Resources.WINDOW_TITLE;
            dialog.InstructionText = "フォルダ削除";
            dialog.Text = string.Format("\"{0}\" を削除しますか？", folderItem.name);
            dialog.Icon = TaskDialogStandardIcon.Warning;
            dialog.StandardButtons = TaskDialogStandardButtons.Yes | TaskDialogStandardButtons.No;
            dialog.OwnerWindowHandle = new WindowInteropHelper(MainWindow.instance).Handle;
            var result = dialog.Show();

            folderItem.isContextMenuCommandTarget = false;

            if (TaskDialogResult.Yes == result)
            {
                // 現在選択されているフォルダが削除される場合、別のフォルダを選択
                if (Object.ReferenceEquals(folderItem, _selectedFolderItem))
                {
                    int index = _folderListItemSource.IndexOf(folderItem);
                    if (index + 1 < _folderListItemSource.Count)
                        ++index;
                    else
                        --index;
                    _folderListView.SelectedIndex = index;
                }

                // 削除
                MainWindow.instance.removedFolderItem(folderItem);

                long folderId = folderItem.id;
                _folderListItemSource.Remove(folderItem);
            }
        }

        private void renameFolder_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            var menu = menuItem.Parent as ContextMenu;

            startEditFolderListItem(this.getAnchorFolderItem(menu));
        }

        #endregion

        #region ■■■■■ 名前変更用 TextBox

        FolderItem _editingFolderItem;
        TextBox _folderListTextBox;

        private void startEditFolderListItem(FolderItem folderItem)
        {
            int index = _folderListItemSource.IndexOf(folderItem);
            var viewItem = _folderListView.ItemContainerGenerator.ContainerFromIndex(index) as ListViewItem;

            // get template
            ContentPresenter myContentPresenter = FindVisualChild<ContentPresenter>(viewItem);
            DataTemplate template = myContentPresenter.ContentTemplate;

            // get controls in template
            var textBox = (template.FindName("textBox", myContentPresenter) as TextBox);
            textBox.Text = folderItem.name;
            textBox.SelectAll();
            textBox.Visibility = Visibility.Visible;
            textBox.Focus();

            _editingFolderItem = folderItem;
            _folderListTextBox = textBox;

            _preventDragFolder = true;
        }

        private void endEditFolderListItem(bool cancel = false)
        {
            _preventDragFolder = false;

            if (null == _editingFolderItem)
                return;

            if (!cancel || _editingFolderItem.id == -1)
            {
                var newName = _folderListTextBox.Text;
                // 前後の空白を削除
                newName = newName.Trim();
                _editingFolderItem.name = newName;
                MainWindow.instance.renamedFolderItem(_editingFolderItem);
            }

            var textBox = _folderListTextBox;

            _editingFolderItem = null;
            _folderListTextBox = null;

            _folderListView.Focus();

            textBox.Visibility = Visibility.Collapsed;
        }

        private childItem FindVisualChild<childItem>(DependencyObject obj)
                 where childItem : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                if (child != null && child is childItem)
                    return (childItem)child;
                else
                {
                    childItem childOfChild = FindVisualChild<childItem>(child);
                    if (childOfChild != null)
                        return childOfChild;
                }
            }
            return null;
        }

        private void folderListTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            this.endEditFolderListItem();
        }

        private void folderListTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                this.endEditFolderListItem();
            }
            else if (e.Key == Key.Escape)
            {
                this.endEditFolderListItem(true);
            }
        }

        #endregion
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
