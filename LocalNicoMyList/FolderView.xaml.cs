using DragDropListView;
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
        public FolderItem _selectedFolderItem;
        ViewModel _viewModel;

        public class ViewModel : ViewModelBase
        {
            public ObservableCollection<FolderItem> _folderLVItemsSource;
            public ObservableCollection<FolderItem> folderLVItemsSource
            {
                get
                {
                    return _folderLVItemsSource;
                }
                set
                {
                    _folderLVItemsSource = value;
                    OnPropertyChanged("folderLVItemsSource");
                }
            }

            private bool _isFolderItemDragEnabled = true;
            public bool isFolderItemDragEnabled
            {
                get {
                    return _isFolderItemDragEnabled;
                }
                set
                {
                    _isFolderItemDragEnabled = value;
                    OnPropertyChanged("isFolderItemDragEnabled");
                }
            }

            public AcceptDropSpecifications dropSpecifications { get; private set; }

            public ViewModel()
            {
                _folderLVItemsSource = new ObservableCollection<FolderItem>();
                this.dropSpecifications = new AcceptDropSpecifications();
            }
        }

        public ObservableCollection<FolderItem> folderLVItemsSourcee
        {
            get { return _viewModel.folderLVItemsSource; }
        }

        public FolderView()
        {
            InitializeComponent();

            _viewModel = new ViewModel();
            _viewModel.dropSpecifications.DragOver += DropSpecifications_DragOver;
            _viewModel.dropSpecifications.DragDrop += DropSpecifications_DragDrop;
            this.rootGrid.DataContext = _viewModel;
        }

        #region ■■■■■ フォルダ一覧 ListView

        private void _folderListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FolderItem item = e.AddedItems[0] as FolderItem;
            _selectedFolderItem = item;

            MainWindow.instance.folderListView_SelectionChanged(item);
        }

        private void folderListView_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // アイテムがない場所をクリックされてもフォーカス移動するようにする。
            _folderListView.Focus();
        }

        private void folderListView_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            // ハンドルすることで右クリックでアイテムが選択されなくなる
            e.Handled = true;
        }

        private void folderListView_DragEnter(object sender, DragEventArgs e)
        {
            var lvi = ((ItemsControl)sender).ContainerFromElement(e.OriginalSource as DependencyObject) as ListViewItem;

            if (null != lvi && e.Data.GetDataPresent(typeof(MyListItem)))
            {
                var folderItem = lvi.DataContext as FolderItem;
                if (folderItem.id != _selectedFolderItem.id)
                {
                    // ドロップ先のフォルダのListViewItemの色を変更
                    folderItem.isMyListItemDropTarget = true;
                }
            }
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

        #endregion

        #region ■■■■■ ドラッグ＆ドロップ関連

        private void DropSpecifications_DragOver(object sender, DragEventArgs e)
        {
            var lvi = ((ItemsControl)sender).ContainerFromElement(e.OriginalSource as DependencyObject) as ListViewItem;

            if (null == lvi)
            {
                e.Effects = DragDropEffects.None;
            }
            else
            {
                if (e.Data.GetDataPresent(typeof(FolderItem)))
                {
                }
                else
                {
                    e.Effects = DragDropEffects.None;
                    if (e.Data.GetDataPresent(typeof(MyListItem)))
                    {
                        var folderItem = lvi.DataContext as FolderItem;

                        if (folderItem.id != _selectedFolderItem.id)
                        {
                            if (0 != (e.KeyStates & DragDropKeyStates.ControlKey))
                                e.Effects = DragDropEffects.Copy;
                            else
                                e.Effects = DragDropEffects.Move;
                            // ドロップ先のフォルダのListViewItemの色を変更
                            //folderItem.isMyListItemDropTarget = true;
                        }
                    }
                }
            }
        }

        private void DropSpecifications_DragDrop(object sender, DragEventArgs e)
        {
            var lvi = ((ItemsControl)sender).ContainerFromElement(e.OriginalSource as DependencyObject) as ListViewItem;
            if (null == lvi)
                return;

            var targetFolderItem = lvi.DataContext as FolderItem;
            if (e.Data.GetDataPresent(typeof(FolderItem)))
            {
                this.folderItemDropped(targetFolderItem, e);
            }
            else if (e.Data.GetDataPresent(typeof(MyListItem)))
            {
                this.myListItemDropped(targetFolderItem, e);
            }
        }

        private void folderItemDropped(FolderItem targetFolderItem, DragEventArgs e)
        {
            var itemsSource = this.folderLVItemsSourcee;
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

        private void myListItemDropped(FolderItem targetFolderItem, DragEventArgs e)
        {
            if (0 != (e.KeyStates & DragDropKeyStates.ControlKey))
                MainWindow.instance.copySelectedMyListItem(targetFolderItem);
            else
                MainWindow.instance.moveSelectedMyListItem(targetFolderItem);

            targetFolderItem.isMyListItemDropTarget = false;
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
                if (null == this.folderLVItemsSourcee.FirstOrDefault((_) => { return _.name.Equals(name); }))
                    break;
                ++num;
            }

            long folderId = -1;// _dbAccessor.addFolder(name, folderListItemSource.Count);
            FolderItem folderItem = new FolderItem(folderId, name);
            this.folderLVItemsSourcee.Add(folderItem);

            int index = this.folderLVItemsSourcee.IndexOf(folderItem);
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

            if (this.folderLVItemsSourcee.Count == 1)
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
                    int index = this.folderLVItemsSourcee.IndexOf(folderItem);
                    if (index + 1 < this.folderLVItemsSourcee.Count)
                        ++index;
                    else
                        --index;
                    _folderListView.SelectedIndex = index;
                }

                // 削除
                MainWindow.instance.removedFolderItem(folderItem);

                long folderId = folderItem.id;
                this.folderLVItemsSourcee.Remove(folderItem);
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
            int index = this.folderLVItemsSourcee.IndexOf(folderItem);
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

            _viewModel.isFolderItemDragEnabled = false;
        }

        private void endEditFolderListItem(bool cancel = false)
        {
            _viewModel.isFolderItemDragEnabled = true;

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
}
