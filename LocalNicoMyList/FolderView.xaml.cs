using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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
using WPF.JoshSmith.ServiceProviders.UI;

namespace LocalNicoMyList
{
    /// <summary>
    /// FolderView.xaml の相互作用ロジック
    /// </summary>
    public partial class FolderView : UserControl
    {
        ListViewDragDropManager<FolderItem> _lvDDMan;
        public ObservableCollection<FolderItem> _folderListItemSource;
        public FolderItem _selectedFolderItem;

        public FolderView()
        {
            InitializeComponent();

            _folderListItemSource = new ObservableCollection<FolderItem>();
            _folderListView.DataContext = _folderListItemSource;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            _lvDDMan = new ListViewDragDropManager<FolderItem>(_folderListView);
            _lvDDMan.ProcessDrop += LvDDMan_ProcessDrop;

        }

        private void LvDDMan_ProcessDrop(object sender, ProcessDropEventArgs<FolderItem> e)
        {
            _folderListItemSource.Move(e.OldIndex, e.NewIndex);
            // DBに保存
            MainWindow.instance._dbAccessor.updateFolderOrderIdx(_folderListItemSource);
        }

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

        #region ■■■■■ フォルダ一覧 ListViewItem

        private void folderListViewItem_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            _folderListView.Focus();
            // ハンドルすることで右クリックでアイテムが選択されなくなる
            e.Handled = true;
        }

        private void folderListViewItem_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(FolderItem)))
                return;

            var lvi = sender as ListViewItem;
            var folderItem = lvi.DataContext as FolderItem;

            if (folderItem.id != _selectedFolderItem.id && e.Data.GetDataPresent(typeof(MyListItem)))
            {
                if (0 != (e.KeyStates & DragDropKeyStates.ControlKey))
                    e.Effects = DragDropEffects.Copy;
                else
                    e.Effects = DragDropEffects.Move;
                // ドロップ先のフォルダのListViewItemの色を変更
                folderItem.isMyListItemDropTarget = true;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void folderListViewItem_DragLeave(object sender, DragEventArgs e)
        {
            var lvi = sender as ListViewItem;
            var folderItem = lvi.DataContext as FolderItem;
            folderItem.isMyListItemDropTarget = false;
        }

        private void folderListViewItem_DragOver(object sender, DragEventArgs e)
        {
            folderListViewItem_DragEnter(sender, e);
        }

        private void folderListViewItem_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(FolderItem)))
                return;

            if (e.Data.GetDataPresent(typeof(MyListItem)))
            {
                var lvi = sender as ListViewItem;
                var folderItem = lvi.DataContext as FolderItem;
                if (0 != (e.KeyStates & DragDropKeyStates.ControlKey))
                    MainWindow.instance.copySelectedMyListItem(folderItem);
                else
                    MainWindow.instance.moveSelectedMyListItem(folderItem);

                folderItem.isMyListItemDropTarget = false;
            }
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

            _lvDDMan.ListView = null;
        }

        private void endEditFolderListItem(bool cancel = false)
        {
            _lvDDMan.ListView = _folderListView;

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
