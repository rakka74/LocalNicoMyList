using Codeplex.Data;
using LocalNicoMyList.nicoApi;
using Microsoft.WindowsAPICodePack.Dialogs;
using SharpHeaderCookie;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
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
        ObservableCollection<MyListItem> _myListItemSource;
        string _cookieHeader;
        NicoApi _nicoApi;
        public DBAccessor _dbAccessor { get; private set; }
        CollectionViewSource _myListItemCVS;
        public ObservableCollection<SortItem> _sortCBItems;

        Task _getflvTask;
        CancellationTokenSource _getflvCTS;
        ConcurrentQueue<string> _getflvQueue;

        ViewModel _viewModel;

        class ViewModel : ViewModelBase
        {
            private MainWindow _outer;

            public ViewModel(MainWindow outer)
            {
                _outer = outer;
            }

            private string _getflvText;
            public string getflvText {
                get { return _getflvText; }
                set
                {
                    _getflvText = value;
                    OnPropertyChanged("getflvText");
                }
            }

            private bool _isCheckedGetflv;
            public bool isCheckedGetflv
            {
                get { return _isCheckedGetflv; }
                set
                {
                    _outer.getflvEnabled = value;
                    _isCheckedGetflv = value;
                    OnPropertyChanged("isCheckedGetflv");
                }
            }

            public void setIsCheckedGetflv(bool value)
            {
                _isCheckedGetflv = value;
                OnPropertyChanged("isCheckedGetflv");
            }

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

        }

        public static MainWindow instance;

        public MainWindow()
        {
            InitializeComponent();

            instance = this;

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

            foreach (FolderRecord folderRecord in folderRecordList)
            {
                int count = _dbAccessor.getMyListCount(folderRecord.id);
                folderView._folderListItemSource.Add(new FolderItem(folderRecord, count));
            }

            // マイリスト一覧初期化
            _myListItemCVS = new CollectionViewSource();
            _myListItemCVS.Source = _myListItemSource;
            _myListItemCVS.Filter += titleFilter;
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

            SortKind sortKind;
            try
            {
                sortKind = (SortKind)Enum.Parse(typeof(SortKind), Properties.Settings.Default.LastSelectedSortKind, true);
            }
            catch(Exception e)
            {
                sortKind = SortKind.CreateTimeDescend;
            }
            this.sortCB.SelectedValue = sortKind;

            _viewModel = new ViewModel(this);
            this.DataContext = _viewModel;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
 
            folderView._folderListView.Focus();

            // 前回終了時のフォルダを選択
            long folderId = Properties.Settings.Default.LastSelectedFolderId;
            var folderItem = folderView._folderListItemSource.FirstOrDefault(_ => { return _.id == folderId; });
            if (null != folderItem)
                folderView._folderListView.SelectedItem = folderItem;
            else
                folderView._folderListView.SelectedIndex = 0;

            this.getflvEnabled = Properties.Settings.Default.IsCheckedGetflv;

            _getflvQueue = new ConcurrentQueue<string>();
            foreach (var item in _dbAccessor.getEmptyGetflvInfo())
            {
                _getflvQueue.Enqueue(item.videoId);
            }

            _getflvCTS = new CancellationTokenSource();
            _getflvTask = this.startGetflvTask();
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
            Properties.Settings.Default.Folder_Width = folderView._folderListView.ActualWidth;
            Properties.Settings.Default.LastSelectedFolderId = folderView._selectedFolderItem.id;
            Properties.Settings.Default.LastSelectedSortKind = Enum.GetName(typeof(SortKind), (SortKind)sortCB.SelectedValue);
            Properties.Settings.Default.IsCheckedGetflv = _viewModel.isCheckedGetflv;

            // ファイルに保存
            Properties.Settings.Default.Save();
        }

         private bool prepareCookie()
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
                        {
                            return true;
                        }
                    }
                }

                var dialog = new TaskDialog();
                dialog.Caption = Properties.Resources.WINDOW_TITLE;
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
            return false;
        }


        private void _videoListView_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("UniformResourceLocator") ||
                e.Data.GetDataPresent("UniformResourceLocatorW"))
            {
                e.Effects = DragDropEffects.Copy;
            }
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
                DateTime? latestCommentTime = null;
                var item = MyListItem.from(res, latestCommentTime, DateTime.Now);
                if (null != item)
                {
                    if (!_dbAccessor.isExistMyListItem(videoId, folderView._selectedFolderItem.id))
                    {
                        _myListItemSource.Add(item);
                        _dbAccessor.addMyListItem(item, folderView._selectedFolderItem.id);
                        GetflvInfoRecord getflvInfo = _dbAccessor.getGetflvInfo(videoId);
                        if (null == getflvInfo)
                        {
                            _dbAccessor.addEmptyGetflvInfo(videoId);
                            _getflvQueue.Enqueue(videoId);
                        }
                        else
                        {
                            item.setGetflv(getflvInfo);
                        }
                        folderView._selectedFolderItem.count = _myListItemSource.Count;
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
                            return !_dbAccessor.isExistMyListItem(videoId, folderView._selectedFolderItem.id);
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
                int count = 0;
                Func<dynamic, Task> action = async (item) =>
                {
                    var videoId = item["item_data"]["video_id"];
                    var createTime = (long)item["create_time"];
                    ThumbInfoResponse res = await _nicoApi.getThumbInfo(videoId);
                    DateTime? latestCommentTime = null;
                    var myListItem = MyListItem.from(res, latestCommentTime, DateTimeExt.fromUnixTime(createTime));
                    if (null != myListItem)
                    {
                        myListItems.Add(myListItem);
                    }
                    Interlocked.Add(ref count, 1);
                    progress.Report(count);
                };

                await jsonItems.ForEachAsync(action, 10, token);

                // DBに追加
                _dbAccessor.addMyListItems(myListItems, folderView._selectedFolderItem.id);
                foreach (var item in myListItems)
                {
                    var getflvInfo = _dbAccessor.getGetflvInfo(item.videoId);
                    if (null == getflvInfo)
                    {
                        _dbAccessor.addEmptyGetflvInfo(item.videoId);
                        _getflvQueue.Enqueue(item.videoId);
                    }
                    else
                    {
                        item.setGetflv(getflvInfo);
                    }
                }
                // ビューモデルに追加
                List<MyListItem> list = _myListItemSource.ToList();
                list.AddRange(myListItems);
                _myListItemSource = new ObservableCollection<MyListItem>(list);
                _myListItemCVS.Source = _myListItemSource;

                folderView._selectedFolderItem.count = list.Count;
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
            if (_myListItemSource.Count == 0)
                return;

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
                        try
                        {
                            latestCommentTime = await _nicoApi.getLatestCommentTimeAsync(item.threadId, item.messageServerUrl);
                            if (null == latestCommentTime)
                            {
                                // コメント数が0の場合、投稿日時をセット
                                latestCommentTime = item.firstRetrieve;
                            }
                        }
                        catch(Exception e)
                        {
                            latestCommentTime = null;
                        }
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
                _dbAccessor.updateMyListItems(_myListItemSource, folderView._selectedFolderItem.id);

                // これをしないと更新後の値でソートされない
                _myListItemCVS.IsLiveSortingRequested = true;
                _myListItemCVS.IsLiveSortingRequested = false;
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

        public bool getflvEnabled
        {
            get { return _viewModel.isCheckedGetflv; }
            set
            {
                if (value)
                {
                    bool ret = true;
                    if (null == _cookieHeader)
                    {
                        ret = this.prepareCookie();
                    }
                    _viewModel.setIsCheckedGetflv(ret);
                }
                else
                {
                    _viewModel.setIsCheckedGetflv(false);
                }
            }
        }

        private async Task startGetflvTask()
        {
            try
            {
                while (!_getflvCTS.IsCancellationRequested)
                {
                    string videoId;
                    if (_getflvQueue.TryPeek(out videoId))
                    {
                        _viewModel.getflvText = string.Format(": {0} | 残り{1}", videoId, _getflvQueue.Count + 1);
                        if (this.getflvEnabled && null != _cookieHeader)
                        {
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
                                    _getflvQueue.TryDequeue(out videoId);
                                    break;
                                }
                                string closed = nameValues["closed"];
                                if (null != closed && closed.Equals("1"))
                                {
                                    // ログアウトされてる
                                    _cookieHeader = null;
                                    _viewModel.setIsCheckedGetflv(false);
                                    break;
                                }
                                string error = nameValues["error"];
                                if (null != error && error.Equals("access_locked"))
                                {
                                    // アクセス制限
                                    Console.WriteLine("accessLocked -> waitTime={0}", waitTime);
                                    int waitSec = waitTime / 1000;
                                    do
                                    {
                                        _viewModel.getflvText = string.Format(": {0} | 残り{1} [アクセス制限:{2}]", videoId, _getflvQueue.Count + 1, waitSec);
                                        await Task.Delay(1000, _getflvCTS.Token);
                                        --waitSec;
                                    } while (waitSec > 0);
                                    _viewModel.getflvText = string.Format(": {0} | 残り{1}", videoId, _getflvQueue.Count + 1);
                                    waitTime += 1000;
                                }
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

        public void folderListView_SelectionChanged(FolderItem item)
        {
            // DBから情報を取得して動画一覧の内容を更新
            _myListItemSource = new ObservableCollection<MyListItem>();
            foreach (var record in _dbAccessor.getMyListItem(item.id))
            {
                _myListItemSource.Add(new MyListItem(record));
            }

            _myListItemCVS.Source = _myListItemSource;
        }

        public void removedFolderItem(FolderItem folderItem)
        {
            _dbAccessor.deleteMyListItems(folderItem.id);
            _dbAccessor.deleteFolder(folderItem.id);
        }

        public void renamedFolderItem(FolderItem folderItem)
        {
            if (folderItem.id == -1)
            {
                long folderId = _dbAccessor.addFolder(folderItem.name, folderView._folderListItemSource.Count);
                folderItem.id = folderId;
            }
            else
            {
                _dbAccessor.updateFolderName(folderItem.id, folderItem.name);
            }
        }

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

        private void removeMyList_Click(object sender, RoutedEventArgs e)
        {
            var selecteedItems = _videoListView.SelectedItems.Cast<MyListItem>();
            var selectedIndices = selecteedItems.Select(_ => _myListItemSource.IndexOf(_)).OrderByDescending(_ => _);
            foreach (var index in selectedIndices)
            {
                MyListItem item = _myListItemSource.ElementAt(index);
                _myListItemSource.RemoveAt(index);
                _dbAccessor.deleteMyListItem(item.videoId, folderView._selectedFolderItem.id);
            }
            folderView._selectedFolderItem.count = _myListItemSource.Count;
        }

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
            _myListItemCVS.IsLiveFilteringRequested = true;
            _myListItemCVS.IsLiveFilteringRequested = false;
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
