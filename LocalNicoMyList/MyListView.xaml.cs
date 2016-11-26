using DragDropListView;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace LocalNicoMyList
{
    /// <summary>
    /// MyListView.xaml の相互作用ロジック
    /// </summary>
    public partial class MyListView : UserControl
    {
        private ObservableCollection<MyListItem> _myListItemSource;
        public ObservableCollection<MyListItem> myListItemSource
        {
            get { return _myListItemSource; }
            set
            {
                _myListItemSource = value;
                _viewModel.myListItemCVS.Source = value;
            }
        }
        public ObservableCollection<SortItem> _sortCBItems;

        MyListViewVM _viewModel;

        class MyListViewVM : ViewModelBase
        {
            CollectionViewSource _myListItemCVS;
            public CollectionViewSource myListItemCVS
            {
                get { return _myListItemCVS; }
                set
                {
                    _myListItemCVS = value;
                    OnPropertyChanged("myListItemCVS");
                }
            }

            public AcceptDropSpecifications dropSpecifications { get; private set; }

            private Visibility _filterOnVisibility = Visibility.Visible;
            public Visibility filterOnVisibility
            {
                get { return _filterOnVisibility; }
                set
                {
                    _filterOnVisibility = value;
                    OnPropertyChanged("filterOnVisibility");
                }
            }

            private Visibility _filterOffVisibility = Visibility.Collapsed;
            public Visibility filterOffVisibility
            {
                get { return _filterOffVisibility; }
                set
                {
                    _filterOffVisibility = value;
                    OnPropertyChanged("filterOffVisibility");
                }
            }

            private string _titleFilterText = "";
            public string titleFilterText
            {
                get { return _titleFilterText; }
                set
                {
                    _titleFilterText = value;
                    OnPropertyChanged("titleFilterText");
                }
            }

            public MyListViewVM()
            {
                _myListItemCVS = new CollectionViewSource();
                _myListItemCVS.Source = new ObservableCollection<MyListItem>();
                this.dropSpecifications = new AcceptDropSpecifications();
            }
        }


        public MyListView()
        {
            InitializeComponent();

            // コンボボックス初期化
            _sortCBItems = new ObservableCollection<SortItem>();
            _sortCBItems.Add(new SortItem("登録が新しい順", SortKind.CreateTimeDescend));
            _sortCBItems.Add(new SortItem("登録が古い順", SortKind.CreateTimeAscend));
            _sortCBItems.Add(new SortItem("タイトル昇順", SortKind.TitleAscend));
            _sortCBItems.Add(new SortItem("タイトル降順", SortKind.TitleDescend));
            _sortCBItems.Add(new SortItem("投稿が新しい順", SortKind.PostTimeDescend));
            _sortCBItems.Add(new SortItem("投稿が古い順", SortKind.PostTimeAscend));
            _sortCBItems.Add(new SortItem("再生が多い順", SortKind.ViewCountDescend));
            _sortCBItems.Add(new SortItem("再生が少ない順", SortKind.ViewCountAscend));
            _sortCBItems.Add(new SortItem("コメントが新しい順", SortKind.LatestCommentTimeDescend));
            _sortCBItems.Add(new SortItem("コメントが古い順", SortKind.LatestCommentTimeAscend));
            _sortCBItems.Add(new SortItem("コメントが多い順", SortKind.CommentCountDescend));
            _sortCBItems.Add(new SortItem("コメントが少ない順", SortKind.CommentCountAscend));
            _sortCBItems.Add(new SortItem("マイリスト登録が多い順", SortKind.MyListCountDescend));
            _sortCBItems.Add(new SortItem("マイリスト登録が少ない順", SortKind.MyListCountAscend));
            _sortCBItems.Add(new SortItem("時間が長い順", SortKind.LengthCountDescend));
            _sortCBItems.Add(new SortItem("時間が短い順", SortKind.LengthCountAscend));

            this.sortCB.DataContext = _sortCBItems;

            _viewModel = new MyListViewVM();
            _viewModel.myListItemCVS.Filter += titleFilter;
            _viewModel.dropSpecifications.DragEnter += DropSpecifications_DragEnter;
            _viewModel.dropSpecifications.DragOver += DropSpecifications_DragOver;
            _viewModel.dropSpecifications.DragDrop += DropSpecifications_DragDrop;
            this.DataContext = _viewModel;
        }

        public void liveSortingRequest()
        {
            _viewModel.myListItemCVS.IsLiveSortingRequested = true;
            _viewModel.myListItemCVS.IsLiveSortingRequested = false;
        }

        #region ■■■■■ マイリスト一覧

        private void videoListView_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // アイテムがない場所をクリックされてもフォーカス移動するようにする。
            _videoListView.Focus();
        }

        private void videoListView_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            // アイテムがない場所をクリックされてもフォーカス移動するようにする。
            _videoListView.Focus();
        }

        #endregion

        #region ■■■■■ マイリスト一覧 ListViewItem

        private void MyListListViewItem_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            ListViewItem lvi = sender as ListViewItem;
            MyListItem myListItem = lvi?.DataContext as MyListItem;
            if (null != myListItem)
            {
                Process.Start(string.Format("http://www.nicovideo.jp/watch/{0}", myListItem.videoId));
            }
        }

        Point? _mouseDownPt = null;
        bool _preventSelectionChangeOnLeftMouseDown;
        DragDropKeyStates _keyStateOnPreviewMouseDown;

        private void MyListListViewItem_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            _preventSelectionChangeOnLeftMouseDown = false;
            if (e.ChangedButton == MouseButton.Left)
            {
                _mouseDownPt = e.GetPosition(_videoListView);

                // 選択されているアイテムのpressで選択されてしまうと複数アイテムのドラッグができないので選択されるのを抑制
                bool multiSelect = _videoListView.SelectedItems.Count > 1;
                _keyStateOnPreviewMouseDown = 0;
                _keyStateOnPreviewMouseDown |= (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)) ? DragDropKeyStates.ControlKey : 0;
                _keyStateOnPreviewMouseDown |= (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)) ? DragDropKeyStates.ShiftKey : 0;
                ListViewItem lvi = sender as ListViewItem;
                MyListItem myListItem = lvi?.DataContext as MyListItem;
                // Shiftが押されている場合は抑制しない
                if (0 != (_keyStateOnPreviewMouseDown & DragDropKeyStates.ShiftKey))
                    return;
                // Ctrlが押されている場合はpressで選択状態変更を抑制
                if (0 != (_keyStateOnPreviewMouseDown & DragDropKeyStates.ControlKey))
                {
                    _preventSelectionChangeOnLeftMouseDown = true;
                    e.Handled = true;
                }
                // Ctrl,Shiftが押されていない場合、複数選択されていて選択されているアイテムがpressされる場合は抑制
                else if (multiSelect && _videoListView.SelectedItems.Contains(myListItem))
                {
                    _preventSelectionChangeOnLeftMouseDown = true;
                    e.Handled = true;
                }
            }
        }

        private void MyListListViewItem_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed || !_mouseDownPt.HasValue)
            {
                return;
            }
            var point = e.GetPosition(_videoListView);
            if (this.checkDistance(point, _mouseDownPt.Value))
            {
                ListViewItem lvi = sender as ListViewItem;
                MyListItem myListItem = lvi?.DataContext as MyListItem;

                // Ctrlを押しながら未選択のアイテムをdownしてmoveした場合、downで選択状態の変更を抑制しているため
                // downされたアイテムが選択状態にならないので、ここで選択状態にする。
                if (0 != (_keyStateOnPreviewMouseDown & DragDropKeyStates.ControlKey))
                {
                    if (!_videoListView.SelectedItems.Contains(myListItem))
                        _videoListView.SelectedItems.Add(myListItem);
                }

                _preventSelectionChangeOnLeftMouseDown = false; // MouseUpで選択状態を変更しないように

                DragDrop.DoDragDrop(lvi, myListItem, DragDropEffects.All);
                _mouseDownPt = null;
                e.Handled = true;
            }
        }

        private void MyListListViewItem_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            _mouseDownPt = null;

            if (e.ChangedButton == MouseButton.Left && _preventSelectionChangeOnLeftMouseDown)
            {
                // Ctrlが押されている場合は抑制したので、ここで選択状態を変更する
                ListViewItem lvi = sender as ListViewItem;
                MyListItem myListItem = lvi?.DataContext as MyListItem;
                if (0 != (_keyStateOnPreviewMouseDown & DragDropKeyStates.ControlKey))
                {
                    if (_videoListView.SelectedItems.Contains(myListItem))
                        _videoListView.SelectedItems.Remove(myListItem);
                    else
                        _videoListView.SelectedItems.Add(myListItem);
                }
                else
                {
                    // Ctrl,Shiftが押されていない場合は抑制したので、ここで選択する
                    _videoListView.SelectedItems.Clear();
                    _videoListView.SelectedItems.Add(myListItem);
                }

            }
        }

        private bool checkDistance(Point x, Point y)
        {
            return Math.Abs(x.X - y.X) >= SystemParameters.MinimumHorizontalDragDistance ||
                Math.Abs(x.Y - y.Y) >= SystemParameters.MinimumVerticalDragDistance;
        }

        #endregion

        #region ■■■■■ ドロップ関連

        private void DropSpecifications_DragEnter(object sender, DragEventArgs e)
        {
            // ListViewItemの中のTextBlockに入ったりしたタイミングでもDragEnterが呼ばれて何も処理しないとドラッグできてしまう状態になるので。
            this.DropSpecifications_DragOver(sender, e);
            e.Handled = true;
        }

        private void DropSpecifications_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("UniformResourceLocator") ||
                e.Data.GetDataPresent("UniformResourceLocatorW"))
            {
                e.Effects = DragDropEffects.Link;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }

        private async void DropSpecifications_DragDrop(object sender, DragEventArgs e)
        {
            string uri = e.Data.GetData(DataFormats.Text)?.ToString();
            if (null == uri) // 念のため
                return;

            Console.WriteLine(uri);
            var match = Regex.Match(uri, @"watch/(sm[0-9]+)");
            if (match.Success)
            {
                string videoId = match.Groups[1].Value;
                await MainWindow.instance.nicoVideoDroped(videoId);
            }
            match = Regex.Match(uri, @"mylist/[0-9]+");
            if (match.Success)
            {
                await MainWindow.instance.nicoMyListDroped("http://www.nicovideo.jp/" + match.Value);
            }
        }

        #endregion


        #region ■■■■■ マイリスト一覧、コンテキストメニュー

        private void removeMyList_Click(object sender, RoutedEventArgs e)
        {
            this.removeSelectedMyListItem();
        }

        public void removeSelectedMyListItem()
        {
            var selecteedItems = _videoListView.SelectedItems.Cast<MyListItem>();
            var selectedIndices = selecteedItems.Select(_ => _myListItemSource.IndexOf(_)).OrderByDescending(_ => _);
            List<MyListItem> removedItems = new List<MyListItem>();
            foreach (var index in selectedIndices)
            {
                MyListItem item = _myListItemSource.ElementAt(index);
                _myListItemSource.RemoveAt(index);
                removedItems.Add(item);
            }

            MainWindow.instance.removedMyListItems(removedItems);
        }

        #endregion

        #region ■■■■■ ソート関連

        private void sortCB_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.setSortKind((SortKind)((ComboBox)sender).SelectedValue);
        }


        private void sortCB_DropDownClosed(object sender, EventArgs e)
        {
            FocusManager.SetFocusedElement(FocusManager.GetFocusScope(_videoListView), _videoListView);
        }

        public void setSortKind(SortKind sortKind)
        {
            _viewModel.myListItemCVS.SortDescriptions.Clear();

            SortDescription sortDescription = new SortDescription();
            switch (sortKind)
            {
                case SortKind.CreateTimeDescend: // 登録が新しい順
                    sortDescription = new SortDescription
                    {
                        PropertyName = "createTime",
                        Direction = ListSortDirection.Descending
                    };
                    break;
                case SortKind.CreateTimeAscend: // 登録が古い順
                    sortDescription = new SortDescription
                    {
                        PropertyName = "createTime",
                        Direction = ListSortDirection.Ascending
                    };
                    break;
                case SortKind.TitleAscend: // タイトル昇順
                    sortDescription = new SortDescription
                    {
                        PropertyName = "title",
                        Direction = ListSortDirection.Ascending
                    };
                    break;
                case SortKind.TitleDescend: // タイトル降順
                    sortDescription = new SortDescription
                    {
                        PropertyName = "title",
                        Direction = ListSortDirection.Descending
                    };
                    break;
                case SortKind.PostTimeDescend: // 投稿が新しい順
                    sortDescription = new SortDescription
                    {
                        PropertyName = "firstRetrieve",
                        Direction = ListSortDirection.Descending
                    };
                    break;
                case SortKind.PostTimeAscend: // 投稿が古い順
                    sortDescription = new SortDescription
                    {
                        PropertyName = "firstRetrieve",
                        Direction = ListSortDirection.Ascending
                    };
                    break;
                case SortKind.ViewCountDescend:
                    sortDescription = new SortDescription
                    {
                        PropertyName = "viewCounter",
                        Direction = ListSortDirection.Descending
                    };
                    break;
                case SortKind.ViewCountAscend:
                    sortDescription = new SortDescription
                    {
                        PropertyName = "viewCounter",
                        Direction = ListSortDirection.Ascending
                    };
                    break;
                case SortKind.LatestCommentTimeDescend:
                    sortDescription = new SortDescription
                    {
                        PropertyName = "latestCommentTime",
                        Direction = ListSortDirection.Descending
                    };
                    break;
                case SortKind.LatestCommentTimeAscend:
                    sortDescription = new SortDescription
                    {
                        PropertyName = "latestCommentTime",
                        Direction = ListSortDirection.Ascending
                    };
                    break;
                case SortKind.CommentCountDescend:
                    sortDescription = new SortDescription
                    {
                        PropertyName = "commentNum",
                        Direction = ListSortDirection.Descending
                    };
                    break;
                case SortKind.CommentCountAscend:
                    sortDescription = new SortDescription
                    {
                        PropertyName = "commentNum",
                        Direction = ListSortDirection.Ascending
                    };
                    break;
                case SortKind.MyListCountDescend:
                    sortDescription = new SortDescription
                    {
                        PropertyName = "mylistCounter",
                        Direction = ListSortDirection.Descending
                    };
                    break;
                case SortKind.MyListCountAscend:
                    sortDescription = new SortDescription
                    {
                        PropertyName = "mylistCounter",
                        Direction = ListSortDirection.Ascending
                    };
                    break;
                case SortKind.LengthCountDescend:
                    sortDescription = new SortDescription
                    {
                        PropertyName = "length",
                        Direction = ListSortDirection.Descending
                    };
                    break;
                case SortKind.LengthCountAscend:
                    sortDescription = new SortDescription
                    {
                        PropertyName = "length",
                        Direction = ListSortDirection.Ascending
                    };
                    break;
            }

            _viewModel.myListItemCVS.SortDescriptions.Add(sortDescription);
        }

        #endregion

        #region ■■■■■ タイトルフィルター

        private void titleFilter_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox targetTextBox = (TextBox)sender;

            if (targetTextBox.Text == "")
            {
                _viewModel.filterOnVisibility = Visibility.Visible;
                _viewModel.filterOffVisibility = Visibility.Collapsed;
            }
            else
            {
                _viewModel.filterOffVisibility = Visibility.Visible;
                _viewModel.filterOnVisibility = Visibility.Collapsed;
            }
            _viewModel.myListItemCVS.IsLiveFilteringRequested = true;
            _viewModel.myListItemCVS.IsLiveFilteringRequested = false;
        }

        private void titleFilter(object sender, FilterEventArgs e)
        {
            MyListItem item = e.Item as MyListItem;
            if (item != null)
            {
                // カタカナ、ひらがな、全角、半角、大文字、小文字を区別なく比較
                CompareInfo ci = CultureInfo.CurrentCulture.CompareInfo;
                int index = ci.IndexOf(item.title, _viewModel.titleFilterText, CompareOptions.IgnoreKanaType | CompareOptions.IgnoreCase | CompareOptions.IgnoreWidth);
                if (0 <= index)
                {
                    e.Accepted = true;
                }
                else
                {
                    e.Accepted = false;
                }
            }
        }

        private void filterOff_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _viewModel.titleFilterText = "";
        }

        #endregion

        private async void refreshButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.myListItemSource.Count == 0)
                return;

            await MainWindow.instance.refreshCurrentFolderInfo();
        }
    }
}
