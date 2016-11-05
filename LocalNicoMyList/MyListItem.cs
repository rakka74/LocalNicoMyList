using LocalNicoMyList.nicoApi;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static LocalNicoMyList.DBAccessor;

namespace LocalNicoMyList
{
    class MyListItem : INotifyPropertyChanged
    {
        public string videoId { get; set; }
        private string _title;
        public string thumbnailUrl { get; set; }
        public DateTime firstRetrieve { get; set; } // 投稿日時
        public TimeSpan length { get; set; }
        private int _viewCounter; // 再生数
        private int _commentNum; // コメント数
        private int _mylistCounter; // マイリスト数
        public DateTime createTime { get; set; } // マイリスト追加日時

        // Declare the event
        public event PropertyChangedEventHandler PropertyChanged;

        // Create the OnPropertyChanged method to raise the event
        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }

        public string title
        {
            get { return _title; }
            set
            {
                _title = value;
                // Call OnPropertyChanged whenever the property is updated
                OnPropertyChanged("title");
            }
        }
        public int viewCounter
        {
            get { return _viewCounter; }
            set
            {
                _viewCounter = value;
                // Call OnPropertyChanged whenever the property is updated
                OnPropertyChanged("viewCounter");
            }
        }
        public int commentNum
        {
            get { return _commentNum; }
            set
            {
                _commentNum = value;
                // Call OnPropertyChanged whenever the property is updated
                OnPropertyChanged("commentNum");
            }
        }
        public int mylistCounter
        {
            get { return _mylistCounter; }
            set
            {
                _mylistCounter = value;
                // Call OnPropertyChanged whenever the property is updated
                OnPropertyChanged("mylistCounter");
            }
        }


#if false
        public string HtmlToDisplay
        {
            get
            {
                return "<html><head><meta http-equiv='Content-Type' content='text/html;charset=UTF-8'></head><body>【480p】 学̵戦̵都̵市ア̵ス̵タ̵リ̵ス̵ク OP2 に中̵毒になる動画 【4:43】</body></html>";
            }
        }
#endif

        private MyListItem()
        {
        }

        public MyListItem(MyListItemRecord record)
        {
            this.videoId = record.videoId;
            this.title = record.title;
            this.thumbnailUrl = record.thumbnailUrl;
            this.firstRetrieve = record.firstRetrieve;
            this.length = record.length;
            this.viewCounter = record.viewCounter;
            this.commentNum = record.commentNum;
            this.mylistCounter = record.mylistCounter;
            this.createTime = record.createTime;
        }

        public static MyListItem from(ThumbInfoResponse thumbInfoRes, DateTime createTime)
        {
            var status = NicoApi.convertStatus(thumbInfoRes.status, thumbInfoRes.error);
            if (NicoApi.Status.OK != status)
                return null;

            var ret = new MyListItem();

            ret.videoId = thumbInfoRes.thumb.video_id;
            ret.title = thumbInfoRes.thumb.title;
            ret.thumbnailUrl = thumbInfoRes.thumb.thumbnail_url;
            ret.firstRetrieve = DateTime.Parse(thumbInfoRes.thumb.first_retrieve);
            ret.length = NicoApi.convertTimeSpan(thumbInfoRes.thumb.length);
            ret.viewCounter = thumbInfoRes.thumb.view_counter;
            ret.commentNum = thumbInfoRes.thumb.comment_num;
            ret.mylistCounter = thumbInfoRes.thumb.mylist_counter;
            ret.createTime = createTime;

            return ret;
        }

        public void update(ThumbInfoResponse thumbInfoRes)
        {
            this.title = thumbInfoRes.thumb.title;
            this.thumbnailUrl = thumbInfoRes.thumb.thumbnail_url;
            this.firstRetrieve = DateTime.Parse(thumbInfoRes.thumb.first_retrieve);
            this.length = NicoApi.convertTimeSpan(thumbInfoRes.thumb.length);
            this.viewCounter = thumbInfoRes.thumb.view_counter;
            this.commentNum = thumbInfoRes.thumb.comment_num;
            this.mylistCounter = thumbInfoRes.thumb.mylist_counter;
        }


    }
}
