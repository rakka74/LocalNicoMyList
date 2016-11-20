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
        string _cookieHeader;
        NicoApi _nicoApi;
        public DBAccessor _dbAccessor { get; private set; }

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

            // ソートCB、初期値復元
            SortKind sortKind;
            try
            {
                sortKind = (SortKind)Enum.Parse(typeof(SortKind), Properties.Settings.Default.LastSelectedSortKind, true);
            }
            catch(Exception e)
            {
                sortKind = SortKind.CreateTimeDescend;
            }
            this.myListView.sortCB.SelectedValue = sortKind;

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
            Properties.Settings.Default.LastSelectedSortKind = Enum.GetName(typeof(SortKind), (SortKind)this.myListView.sortCB.SelectedValue);
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
                List<MyListItem> list = this.myListView.myListItemSource.ToList();
                list.AddRange(myListItems);
                this.myListView.myListItemSource = new ObservableCollection<MyListItem>(list);

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

        public async Task refreshCurrentFolderInfo()
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

                    MyListItem myListItem = this.myListView.myListItemSource.First(_ => _.videoId.Equals(videoId));
                    myListItem.update(res, latestCommentTime);

                    Interlocked.Add(ref count, 1);
                    progress.Report(count);
                };

                List<MyListItem> myListItems = this.myListView.myListItemSource.ToList();
                progressWindow.ProgressBar.Maximum = myListItems.Count;

                await myListItems.ForEachAsync(action, 10, token);

                // DBを更新
                _dbAccessor.updateMyListItems(this.myListView.myListItemSource, folderView._selectedFolderItem.id);

                // これをしないと更新後の値でソートされない
                myListView.liveSortingRequest();
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
                                    this.myListView.myListItemSource.FirstOrDefault((_) => { return _.videoId.Equals(videoId); })?.setGetflv(threadId, messageServerUrl);
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

        public void folderListView_SelectionChanged(FolderItem item)
        {
            // DBから情報を取得して動画一覧の内容を更新
            var newSrc = new ObservableCollection<MyListItem>();
            foreach (var record in _dbAccessor.getMyListItem(item.id))
            {
                newSrc.Add(new MyListItem(record));
            }

            this.myListView.myListItemSource = newSrc;
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

        public async Task nicoVideoDroped(string videoId)
        {
            ThumbInfoResponse res = await _nicoApi.getThumbInfo(videoId);
            DateTime? latestCommentTime = null;
            var item = MyListItem.from(res, latestCommentTime, DateTime.Now);
            if (null != item)
            {
                if (!_dbAccessor.isExistMyListItem(videoId, folderView._selectedFolderItem.id))
                {
                    this.myListView.myListItemSource.Add(item);
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
                    folderView._selectedFolderItem.count = this.myListView.myListItemSource.Count;
                }
            }
        }

        public async Task nicoMyListDroped(string url)
        {
            var hc = new HttpClient();
            string ret = await hc.GetStringAsync(url);
            var match = Regex.Match(ret, @"Mylist.preload\([0-9]+,(.*?)\);");
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

        public void removedMyListItems(List<MyListItem> removedItems)
        {
            foreach (var item in removedItems)
            {
                _dbAccessor.deleteMyListItem(item.videoId, folderView._selectedFolderItem.id);
            }
            folderView._selectedFolderItem.count = this.myListView.myListItemSource.Count;
        }

        public void moveSelectedMyListItem(FolderItem folderItem)
        {
            this.copySelectedMyListItem(folderItem);

            this.myListView.removeSelectedMyListItem();
        }

        public void copySelectedMyListItem(FolderItem folderItem)
        {
            var myListItems = this.myListView._videoListView.SelectedItems;
            // 追加されていないものだけを列挙
            var myListItems2 = myListItems.Cast<MyListItem>().Where((item) => {
                var videoId = item.videoId;
                return !_dbAccessor.isExistMyListItem(videoId, folderItem.id);
            });
            // DBに追加
            _dbAccessor.addMyListItems(myListItems2, folderItem.id);
            // フォルダ内アイテム数の表示を更新
            folderItem.count = _dbAccessor.getMyListCount(folderItem.id);
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
