using Codeplex.Data;
using LocalNicoMyList.nicoApi;
using Microsoft.WindowsAPICodePack.Dialogs;
using SharpHeaderCookie;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using WPF.JoshSmith.ServiceProviders.UI;
using static LocalNicoMyList.DBAccessor;
using static LocalNicoMyList.nicoApi.NicoApi;

namespace LocalNicoMyList
{
#if false
    public static class BrowserBehavior
    {
        public static readonly DependencyProperty HtmlProperty = DependencyProperty.RegisterAttached(
            "Html",
            typeof(string),
            typeof(BrowserBehavior),
            new FrameworkPropertyMetadata(OnHtmlChanged));

        [AttachedPropertyBrowsableForType(typeof(WebBrowser))]
        public static string GetHtml(WebBrowser d)
        {
            return (string)d.GetValue(HtmlProperty);
        }

        public static void SetHtml(WebBrowser d, string value)
        {
            d.SetValue(HtmlProperty, value);
        }

        static void OnHtmlChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            WebBrowser wb = d as WebBrowser;
            if (wb != null)
                wb.NavigateToString(e.NewValue as string);
        }
    }
#endif

    public enum SortKind
    {
        CreateTimeDescend,  // 登録が新しい順
        CreateTimeAscend,   // 登録が古い順
        TitleAscend,        // タイトル昇順
        TitleDescend,       // タイトル降順
        PostTimeDescend,    // 投稿が新しい順
        PostTimeAscend,     // 投稿が古い順
        ViewCountDescend,   // 再生が多い順
        ViewCountAscend,    // 再生が少ない順
        LatestCommentTimeDescend,   // コメントが新しい順
        LatestCommentTimeAscend,    // コメントが古い順
        CommentCountDescend,    // コメントが多い順
        CommentCountAscend,     // コメントが少ない順
        MyListCountDescend, // マイリスト登録が多い順
        MyListCountAscend,  // マイリスト登録が少ない順
        LengthCountDescend, // 時間が長い順
        LengthCountAscend   // 時間が短い順
    }

    public class SortItem
    {
        public string name { get; set; }
        public SortKind id { get; set; }

        public SortItem(string name, SortKind id)
        {
            this.name = name;
            this.id = id;
        }
    }

    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        ObservableCollection<FolderItem> _folderListItemSource;
        ObservableCollection<MyListItem> _myListItemSource;
        string _cookieHeader;
        NicoApi _nicoApi;
        DBAccessor _dbAccessor;
        FolderItem _selectedFolderItem;
        CollectionViewSource _myListItemCVS;
        public ObservableCollection<SortItem> _sortCBItems;

        Task _getflvTask;
        CancellationTokenSource _getflvCTS;
        ConcurrentQueue<string> _getflvQueue;

        ViewModel _viewModel;

        class ViewModel : ViewModelBase
        {
            private string _getflvText;
            public string getflvText {
                get { return _getflvText; }
                set
                {
                    _getflvText = value;
                    OnPropertyChanged("getflvText");
                }
            }
        }

        public MainWindow()
        {
            InitializeComponent();

            if (0 != Properties.Settings.Default.MainWindow_Left)
            {
                Left = Properties.Settings.Default.MainWindow_Left;
                Top = Properties.Settings.Default.MainWindow_Top;
                Width = Properties.Settings.Default.MainWindow_Width;
                Height = Properties.Settings.Default.MainWindow_Height;
            }
            if (0 != Properties.Settings.Default.Folder_Width)
            {
                _Grid1.ColumnDefinitions[0].Width = new GridLength(Properties.Settings.Default.Folder_Width);
            }

            _nicoApi = new NicoApi();
            _dbAccessor = new DBAccessor();

            // フォルダ一覧初期化
            List<FolderRecord> folderRecordList = _dbAccessor.getFolder();
            long id;
            if (0 == folderRecordList.Count)
            {
                id = _dbAccessor.addFolder("フォルダ1", 0);
            }
            folderRecordList = _dbAccessor.getFolder();

            _folderListItemSource = new ObservableCollection<FolderItem>();
            foreach (FolderRecord folderRecord in folderRecordList)
            {
                int count = _dbAccessor.getMyListCount(folderRecord.id);
                _folderListItemSource.Add(new FolderItem(folderRecord, count));
            }

            _folderListView.DataContext = _folderListItemSource;

            // マイリスト一覧初期化
            _myListItemCVS = new CollectionViewSource();
            _myListItemCVS.Source = _myListItemSource;
            _videoListView.DataContext = _myListItemCVS;

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

            this.sortCB.SelectedValue = SortKind.ViewCountDescend; // とりあえず再生数の多い順で表示

            _viewModel = new ViewModel();
            this.DataContext = _viewModel;

        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var lvDDMan = new ListViewDragDropManager<FolderItem>(_folderListView);
            lvDDMan.ProcessDrop += LvDDMan_ProcessDrop;

            _folderListView.Focus();
            _folderListView.SelectedIndex = 0;

            this.prepareCookie();

            _getflvQueue = new ConcurrentQueue<string>();
            foreach (var item in _dbAccessor.getEmptyGetflvInfo())
            {
                _getflvQueue.Enqueue(item.videoId);
            }

            _getflvCTS = new CancellationTokenSource();
            _getflvTask = this.startGetflvTask();
        }

        private void LvDDMan_ProcessDrop(object sender, ProcessDropEventArgs<FolderItem> e)
        {
            _folderListItemSource.Move(e.OldIndex, e.NewIndex);
            // DBに保存
            _dbAccessor.updateFolderOrderIdx(_folderListItemSource);
        }

        private async void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // getflvのバックグランド処理をキャンセルして終了待機。待機する必要ないかも？
            _getflvCTS.Cancel();
            await _getflvTask;

            _dbAccessor.Dispose();

            // ウィンドウの値を Settings に格納
            Properties.Settings.Default.MainWindow_Left = Left;
            Properties.Settings.Default.MainWindow_Top = Top;
            Properties.Settings.Default.MainWindow_Width = Width;
            Properties.Settings.Default.MainWindow_Height = Height;
            Properties.Settings.Default.Folder_Width = _folderListView.ActualWidth;
            // ファイルに保存
            Properties.Settings.Default.Save();
        }

         private void prepareCookie()
        {
            while (true) {
                IGetBrowserCookie[] getBrowserCookies = LibHeaderCookie.Instance();
                Uri uri;
                foreach (IGetBrowserCookie getBrowserCookie in getBrowserCookies)
                {
                    if (Uri.TryCreate("http://live.nicovideo.jp/", UriKind.Absolute, out uri))
                    {
                        _cookieHeader = getBrowserCookie.CookieHeader(uri, "user_session");
                        if (null != _cookieHeader)
                            return;
                    }
                }

                var dialog = new TaskDialog();
                dialog.Caption = "LocalNicoMyList";
                dialog.InstructionText = "クッキーの取得ができませんでした。";
                dialog.Text = "最新コメント日時を取得するのにニコニコ動画にログインしているクッキーが必要になります。\n" +
                    "適当なブラウザでニコニコ動画にログインした後、再試行ボタンを押してください。";
                dialog.Icon = TaskDialogStandardIcon.Information;
                dialog.StandardButtons = TaskDialogStandardButtons.Retry | TaskDialogStandardButtons.Cancel;
                dialog.OwnerWindowHandle = new WindowInteropHelper(this).Handle;
                var result = dialog.Show();
                if (TaskDialogResult.Cancel == result)
                {
                    break;
                }
            }
        }


        private void _videoListView_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("UniformResourceLocator") ||
                e.Data.GetDataPresent("UniformResourceLocatorW"))
            {
                e.Effects = DragDropEffects.Copy;
            }
        }

        private async Task<DateTime?> getLatestCommentTimeAsync(string videoId)
        {
            DateTime? latestCommentTime = null;
            if (null != _cookieHeader)
            {
                latestCommentTime = await _nicoApi.getLatestCommentTimeAsync(videoId, _cookieHeader);
                if (!latestCommentTime.HasValue)
                {
                    // メッセージでも表示する？
                    _cookieHeader = null;
                }
            }
            return latestCommentTime;
        }

        private async void _videoListView_Drop(object sender, DragEventArgs e)
        {
            string uri = e.Data.GetData(DataFormats.Text)?.ToString();
            Console.WriteLine(uri);
            var match = Regex.Match(uri, @"watch/(sm[0-9]+)");
            if (match.Success)
            {
                string videoId = match.Groups[1].Value;
                ThumbInfoResponse res = await _nicoApi.getThumbInfo(videoId);
                DateTime? latestCommentTime = null;// await this.getLatestCommentTimeAsync(videoId);
                var item = MyListItem.from(res, latestCommentTime, DateTime.Now);
                if (null != item)
                {
                    if (!_dbAccessor.isExistMyListItem(videoId, _selectedFolderItem.id))
                    {
                        _myListItemSource.Add(item);
                        _dbAccessor.addMyListItem(item, _selectedFolderItem.id);
                        if (!_dbAccessor.isExistGetflvInfo(videoId))
                        {
                            _dbAccessor.addEmptyGetflvInfo(videoId);
                            _getflvQueue.Enqueue(videoId);
                        }
                        _selectedFolderItem.count = _myListItemSource.Count;
                    }
                }
            }
            match = Regex.Match(uri, @"mylist/[0-9]+");
            if (match.Success)
            {
                var hc = new HttpClient();
                string ret = await hc.GetStringAsync("http://www.nicovideo.jp/" + match.Value);
                match = Regex.Match(ret, @"Mylist.preload\([0-9]+,(.*?)\);");
                if (match.Success)
                {
                    var jsonStr = match.Groups[1].Value;
                    try
                    {
                        dynamic json = DynamicJson.Parse(jsonStr);

                        var progressWindow = new ProgressWindow();
                        var progress = new Progress<int>(value =>
                        {
                            progressWindow.ProgressBar.Value = value;
                        });

                        var cts = new CancellationTokenSource();
                        progressWindow.Closed += (_, __) => cts.Cancel();

                        var jsonItems = (dynamic[])json;
                        var jsonItems2 = jsonItems.Where((item) => {
                            var videoId = item["item_data"]["video_id"];
                            return !_dbAccessor.isExistMyListItem(videoId, _selectedFolderItem.id);
                        });


                        progressWindow.ProgressBar.Maximum = (double)jsonItems2.Count();
                        var task = importMyListAsync(jsonItems2, progressWindow, cts.Token, progress);
                        progressWindow.ShowDialog();
                        await task;
                    }
                    catch (Exception exp)
                    {
                        Console.WriteLine(exp);
                    }
                }
            }
        }

        // jsonに含まれるマイリスト情報をカレントフォルダに追加。
        // DBとビューモデルに追加。
        async Task importMyListAsync(IEnumerable<dynamic> jsonItems, ProgressWindow progressWindow, CancellationToken token, IProgress<int> progress)
        {
            try
            {
                var myListItems = new System.Collections.Concurrent.ConcurrentBag<MyListItem>();
                Func<dynamic, Task> action = async (item) =>
                {
                    var videoId = item["item_data"]["video_id"];
                    var createTime = (long)item["create_time"];
                    ThumbInfoResponse res = await _nicoApi.getThumbInfo(videoId);
                    DateTime? latestCommentTime = null;// await this.getLatestCommentTimeAsync(videoId);
                    var myListItem = MyListItem.from(res, latestCommentTime, DateTimeExt.fromUnixTime(createTime));
                    if (null != myListItem)
                    {
                        myListItems.Add(myListItem);
                        progress.Report(myListItems.Count);
                    }
                };

                await jsonItems.ForEachAsync(action, 10, token);

                // DBに追加
                _dbAccessor.addMyListItems(myListItems, _selectedFolderItem.id);
                foreach (var item in myListItems)
                {
                    if (!_dbAccessor.isExistGetflvInfo(item.videoId))
                    {
                        _dbAccessor.addEmptyGetflvInfo(item.videoId);
                        _getflvQueue.Enqueue(item.videoId);
                    }
                }
                // ビューモデルに追加
                List<MyListItem> list = _myListItemSource.ToList();
                list.AddRange(myListItems);
                _myListItemSource = new ObservableCollection<MyListItem>(list);
                _myListItemCVS.Source = _myListItemSource;

                _selectedFolderItem.count = list.Count;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                progressWindow.Close();
            }
        }

        private async void refreshButton_Click(object sender, RoutedEventArgs e)
        {
            var progressWindow = new ProgressWindow();
            var progress = new Progress<int>(value =>
            {
                progressWindow.ProgressBar.Value = value;
            });

            var cts = new CancellationTokenSource();
            progressWindow.Closed += (_, __) => cts.Cancel();

            var task = refreshMyListAsync(progressWindow, cts.Token, progress);
            progressWindow.ShowDialog();
            await task;
        }

        // カレントフォルダの動画情報を取得し直す
        private async Task refreshMyListAsync(ProgressWindow progressWindow, CancellationToken token, IProgress<int> progress)
        {
            try
            {
                int count = 0;
                Func<MyListItem, Task> action = async (item) =>
                {
                    var videoId = item.videoId;
                    ThumbInfoResponse res = await _nicoApi.getThumbInfo(videoId);
                    DateTime? latestCommentTime = null;
                    if (null != item.threadId)
                    {
                        latestCommentTime = await _nicoApi.getLatestCommentTimeAsync(item.threadId, item.messageServerUrl);
                    }

                    MyListItem myListItem = _myListItemSource.First(_ => _.videoId.Equals(videoId));
                    myListItem.update(res, latestCommentTime);

                    Interlocked.Add(ref count, 1);
                    progress.Report(count);
                };

                List<MyListItem> myListItems = _myListItemSource.ToList();
                progressWindow.ProgressBar.Maximum = myListItems.Count;

                await myListItems.ForEachAsync(action, 10, token);

                // DBを更新
                _dbAccessor.updateMyListItems(_myListItemSource, _selectedFolderItem.id);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                progressWindow.Close();
            }
        }

        private async Task startGetflvTask()
        {
            try
            {
                while (!_getflvCTS.IsCancellationRequested)
                {
                    string videoId;
                    if (null != _cookieHeader && _getflvQueue.TryDequeue(out videoId))
                    {
                        _viewModel.getflvText = string.Format("getflv: {1} | 残り{0}", _getflvQueue.Count + 1, videoId);
                        int waitTime = 1000 * 30;
                        while (!_getflvCTS.IsCancellationRequested)
                        {
                            Console.WriteLine(videoId);
                            NameValueCollection nameValues = await _nicoApi.getflvAsync(videoId, _cookieHeader);
                            string threadId = nameValues.Get("thread_id");
                            string messageServerUrl = nameValues.Get("ms");
                            if (null != threadId && null != messageServerUrl)
                            {
                                _dbAccessor.updateGetflvInfo(videoId, threadId, messageServerUrl);
                                _myListItemSource.FirstOrDefault((_) => { return _.videoId.Equals(videoId); })?.setGetflv(threadId, messageServerUrl);
                                break;
                            }
                            string closed = nameValues["closed"];
                            if (null != closed && closed.Equals("1"))
                            {
                                // ログアウトされてる
                                _cookieHeader = null;
                                _getflvQueue.Enqueue(videoId);
                                break;
                            }
                            string error = nameValues["error"];
                            if (null != error && error.Equals("access_locked"))
                            {
                                // アクセス制限
                                Console.WriteLine("accessLocked -> waitTime={0}", waitTime);
                                await Task.Delay(waitTime, _getflvCTS.Token);
                                waitTime += 1000;
                            }
                        }
                    }
                    else
                    {
                        _viewModel.getflvText = "";
                    }
                    await Task.Delay(1000, _getflvCTS.Token);
                }
            }
            catch(TaskCanceledException e)
            {
            }
            catch(Exception e2)
            {
                Console.WriteLine(e2);
            }
        }

        private void _folderListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FolderItem item = e.AddedItems[0] as FolderItem;
            _selectedFolderItem = item;

            // DBから情報を取得して動画一覧の内容を更新
            _myListItemSource = new ObservableCollection<MyListItem>();
            foreach (var record in _dbAccessor.getMyListItem(item.id))
            {
                _myListItemSource.Add(new MyListItem(record));
            }

            _myListItemCVS.Source = _myListItemSource;
        }

        private void sortCB_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.setSortKind((SortKind)((ComboBox)sender).SelectedValue);
        }

        public void setSortKind(SortKind sortKind)
        {
            _myListItemCVS.SortDescriptions.Clear();

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
                        PropertyName = "postTime",
                        Direction = ListSortDirection.Descending
                    };
                    break;
                case SortKind.PostTimeAscend: // 投稿が古い順
                    sortDescription = new SortDescription
                    {
                        PropertyName = "postTime",
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

            _myListItemCVS.SortDescriptions.Add(sortDescription);
        }

        private void MyListListViewItem_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            ListViewItem lvi = sender as ListViewItem;
            MyListItem myListItem = lvi?.DataContext as MyListItem;
            if (null != myListItem)
            {
                Process.Start(string.Format("http://www.nicovideo.jp/watch/{0}", myListItem.videoId));
            }
        }

        private void addFolder_Click(object sender, RoutedEventArgs e)
        {
            this.addFolder();
        }

        private FolderItem getAnchorFolderItem(ContextMenu menu)
        {
            ListViewItem listViewItem = menu.PlacementTarget as ListViewItem;
            FolderItem folderItem = listViewItem?.Content as FolderItem;
            return folderItem;
        }

        private void removeFolder_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            var menu = menuItem.Parent as ContextMenu;
            this.removeFolder(this.getAnchorFolderItem(menu));
        }

        private void renameFolder_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            var menu = menuItem.Parent as ContextMenu;
            this.renameFolder(this.getAnchorFolderItem(menu));
        }

        private void addFolder()
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

            long folderId = _dbAccessor.addFolder(name, _folderListItemSource.Count);
            FolderItem folderItem = new FolderItem(folderId, name);
            _folderListItemSource.Add(folderItem);

            int index = _folderListItemSource.IndexOf(folderItem);
            this.Dispatcher.BeginInvoke((Action)(async () =>
            {
                while(true)
                {
                    var viewItem = _folderListView.ItemContainerGenerator.ContainerFromIndex(index) as ListViewItem;
                    if (null != viewItem) break;
                    await Task.Delay(100);
                }
                startEditFolderListItem(folderItem);
            }));
        }

        private void removeFolder(FolderItem folderItem)
        {
            if (_folderListItemSource.Count == 1)
                return;

            folderItem.isContextMenuCommandTarget = true;

            var dialog = new TaskDialog();
            dialog.Caption = "LocalNicoMyList";
            dialog.InstructionText = "フォルダ削除";
            dialog.Text = string.Format("\"{0}\" を削除しますか？", folderItem.name);
            dialog.Icon = TaskDialogStandardIcon.Warning;
            dialog.StandardButtons = TaskDialogStandardButtons.Yes | TaskDialogStandardButtons.No;
            dialog.OwnerWindowHandle = new WindowInteropHelper(this).Handle; 
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
                long folderId = folderItem.id;
                _dbAccessor.deleteMyListItems(folderId);
                _dbAccessor.deleteFolder(folderId);
                _folderListItemSource.Remove(folderItem);
            }
        }

        private void renameFolder(FolderItem folderItem)
        {
            startEditFolderListItem(folderItem);
        }

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
        }

        private void endEditFolderListItem(bool cancel = false)
        {
            if (null == _editingFolderItem)
                return;

            if (!cancel)
            {
                var newName = _folderListTextBox.Text;
                // 前後の空白を削除
                newName = newName.Trim();
                _editingFolderItem.name = newName;
                _dbAccessor.updateFolderName(_editingFolderItem.id, newName);
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
            e.Handled = true;
        }

        FolderItem _cotextMenuFolderItem;

        private void ListViewItem_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            _folderListView.Focus();
            e.Handled = true;
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
    }

    public static class EnumerableExtensions
    {
        public static async Task ForEachAsync<T>(this IEnumerable<T> source, Func<T, Task> action, int concurrency, CancellationToken cancellationToken = default(CancellationToken), bool configureAwait = false)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (action == null) throw new ArgumentNullException("action");
            if (concurrency <= 0) throw new ArgumentOutOfRangeException("concurrencyは1以上の必要があります");

            using (var semaphore = new SemaphoreSlim(initialCount: concurrency, maxCount: concurrency))
            {
                var exceptionCount = 0;
                var tasks = new List<Task>();

                try
                {
                    foreach (var item in source)
                    {
                        if (exceptionCount > 0)
                            break;
                        cancellationToken.ThrowIfCancellationRequested();
                        await semaphore.WaitAsync(cancellationToken).ConfigureAwait(configureAwait);
                        var task = action(item).ContinueWith(t =>
                        {
                            semaphore.Release();

                            if (t.IsFaulted)
                            {
                                Interlocked.Increment(ref exceptionCount);
                                throw t.Exception;
                            }
                        });
                        tasks.Add(task);
                    }
                }
                catch (Exception exp)
                {
                    Console.WriteLine(exp);
                }

                await Task.WhenAll(tasks.ToArray()).ConfigureAwait(configureAwait);
            }
        }
    }
}
