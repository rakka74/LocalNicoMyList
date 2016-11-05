using Codeplex.Data;
using LocalNicoMyList.nicoApi;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
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
using static LocalNicoMyList.DBAccessor;

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

    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        ObservableCollection<FolderItem> _folderListItemSource;
        ObservableCollection<MyListItem> _myListItemSource;
        NicoApi _nicoApi;
        DBAccessor _dbAccessor;
        //        long _selectedFolderId;
        FolderItem _selectedFolderItem;
        CollectionViewSource _myListItemCVS;

        //class VideoInfo
        //{

        //}

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

            List<FolderRecord> folderRecordList = _dbAccessor.getFolder();
            long id;
            if (0 == folderRecordList.Count)
            {
                id = _dbAccessor.addFolder("フォルダ1");
            }
            folderRecordList = _dbAccessor.getFolder();

            _folderListItemSource = new ObservableCollection<FolderItem>();
            foreach (FolderRecord folderRecord in folderRecordList)
            {
                _folderListItemSource.Add(new FolderItem(folderRecord));
            }

            _folderListView.DataContext = _folderListItemSource;

            _myListItemCVS = new CollectionViewSource();
            _myListItemCVS.Source = _myListItemSource;
            _videoListView.DataContext = _myListItemCVS;

            // とりあえず再生数でソート
            var sortDescription = new SortDescription
            {
                PropertyName = "viewCounter",
                Direction = ListSortDirection.Ascending
            };
            _myListItemCVS.SortDescriptions.Add(sortDescription);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _folderListView.SelectedIndex = 0;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
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
                var item = MyListItem.from(res, DateTime.Now);
                if (null != item)
                {
                    _myListItemSource.Add(item);
                    _dbAccessor.addMyListItem(item, _selectedFolderItem.id);

                    _selectedFolderItem.count = _myListItemSource.Count;
                    _dbAccessor.updateCount(_selectedFolderItem.id, _myListItemSource.Count);
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
                        progressWindow.ProgressBar.MaxHeight = jsonItems.Length;
                        var task = importMyListAsync(jsonItems, progressWindow, cts.Token, progress);
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
        async Task importMyListAsync(dynamic[] jsonItems, ProgressWindow progressWindow, CancellationToken token, IProgress<int> progress)
        {
            try
            {
                var myListItems = new System.Collections.Concurrent.ConcurrentBag<MyListItem>();
                Func<dynamic, Task> action = async (item) =>
                {
                    var videoId = item["item_data"]["video_id"];
                    var createTime = (long)item["create_time"];
                    ThumbInfoResponse res = await _nicoApi.getThumbInfo(videoId);
                    var myListItem = MyListItem.from(res, DateTimeExt.fromUnixTime(createTime));
                    if (null != myListItem)
                    {
                        myListItems.Add(myListItem);
                        progress.Report(myListItems.Count);
                    }
                };

                await jsonItems.ForEachAsync(action, 10, token);

                // DBに追加
                _dbAccessor.addMyListItems(myListItems, _selectedFolderItem.id);
                // ビューモデルに追加
                List<MyListItem> list = _myListItemSource.ToList();
                list.AddRange(myListItems);
                _myListItemSource = new ObservableCollection<MyListItem>(list);
                _myListItemCVS.Source = _myListItemSource;

                _selectedFolderItem.count = list.Count;
                _dbAccessor.updateCount(_selectedFolderItem.id, list.Count);

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

        private async void _folderListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FolderItem item = e.AddedItems[0] as FolderItem;
            _selectedFolderItem = item;
            //            _selectedFolderId = item.id;
            await refreshMyList();
        }

        CancellationTokenSource _refreshMyListCts;

        // カレントフォルダのマイリストの動画情報を取得し直す
        async Task refreshMyList()
        {
            _myListItemCVS.Source = null;  // 表示をクリア

            List<MyListItemRecord> ret = _dbAccessor.getMyListItem(_selectedFolderItem.id);

            //var myListItems = new ObservableCollection<MyListItem>();
            var myListItems = new System.Collections.Concurrent.ConcurrentBag<MyListItem>();

            // ニコニコ動画APIで動画情報を取得
            Func<MyListItemRecord, Task> action = async (myListItemRecord) =>
            {
                var videoId = myListItemRecord.videoId;
                var res = await _nicoApi.getThumbInfo(videoId);
                var myListItem = MyListItem.from(res, myListItemRecord.createTime);
                if (null != myListItem)
                {
                    myListItems.Add(myListItem);
                }
            };

            if (null != _refreshMyListCts)
            {
                _refreshMyListCts.Cancel();
            }

            _refreshMyListCts = new CancellationTokenSource();
            var token = _refreshMyListCts.Token;
            await ret.ForEachAsync(action, 10, token);

            if (token.IsCancellationRequested)
                return;

            _myListItemSource = new ObservableCollection<MyListItem>(myListItems);
            _myListItemCVS.Source = _myListItemSource;
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
