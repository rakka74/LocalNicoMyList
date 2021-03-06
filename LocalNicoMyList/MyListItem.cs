﻿using LocalNicoMyList.nicoApi;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static LocalNicoMyList.DBAccessor;

namespace LocalNicoMyList
{
    public class MyListItem : ViewModelBase
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
        private DateTime? _latestCommentTime; // 最新コメント日時
        private string _threadId;
        private string _messageServerUrl;

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

        public string lengthText
        {
            get
            {
                return string.Format("{0}:{1:D2}", Math.Floor(this.length.TotalMinutes), this.length.Seconds);
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
        public DateTime? latestCommentTime
        {
            get { return _latestCommentTime; }
            set
            {
                _latestCommentTime = value;
                // Call OnPropertyChanged whenever the property is updated
                OnPropertyChanged("latestCommentTime");
            }
        }
        public string getflv
        {
            get { return (null != _threadId) ? "○" : "×"; }
        }
        public void setGetflv(string threadId, string ms)
        {
            _threadId = threadId;
            _messageServerUrl = ms;
            // Call OnPropertyChanged whenever the property is updated
            OnPropertyChanged("getflv");
        }
        public void setGetflv(GetflvInfoRecord getflv)
        {
            this.setGetflv(getflv.threadId, getflv.messageServerUrl);
        }

        public string threadId { get { return _threadId; } }
        public string messageServerUrl { get { return _messageServerUrl; } }



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
            this.latestCommentTime = record.latestCommentTime;
            this.setGetflv(record.threadId, record.messageServerUrl);
        }

        public static MyListItem from(ThumbInfoResponse thumbInfoRes, DateTime? latestCommentTime, DateTime createTime)
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
            ret.latestCommentTime = latestCommentTime;
            ret.createTime = createTime;

            return ret;
        }

        public void update(ThumbInfoResponse thumbInfoRes, DateTime? latestCommentTime)
        {
            this.title = thumbInfoRes.thumb.title;
            this.thumbnailUrl = thumbInfoRes.thumb.thumbnail_url;
            this.firstRetrieve = DateTime.Parse(thumbInfoRes.thumb.first_retrieve);
            this.length = NicoApi.convertTimeSpan(thumbInfoRes.thumb.length);
            this.viewCounter = thumbInfoRes.thumb.view_counter;
            this.commentNum = thumbInfoRes.thumb.comment_num;
            this.mylistCounter = thumbInfoRes.thumb.mylist_counter;
            if (latestCommentTime.HasValue)
                this.latestCommentTime = latestCommentTime;
        }


    }
}
