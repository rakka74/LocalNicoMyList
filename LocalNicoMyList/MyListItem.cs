using LocalNicoMyList.nicoApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LocalNicoMyList
{
    class MyListItem
    {
        public string videoId { get; set; }
        public string title { get; set; }
        public string thumbnailUrl { get; set; }
        public DateTime postTime { get; set; } // 投稿日時
        public TimeSpan length { get; set; }
        public int viewCounter { get; set; } // 再生数
        public int commentNum { get; set; } // コメント数
        public int mylistCounter { get; set; } // マイリスト数
        public DateTime createTime { get; set; } // マイリスト追加日時

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

        public static MyListItem from(ThumbInfoResponse thumbInfoRes, DateTime createTime)
        {
            var status = NicoApi.convertStatus(thumbInfoRes.status, thumbInfoRes.error);
            if (NicoApi.Status.OK != status)
                return null;

            var ret = new MyListItem();

            ret.videoId = thumbInfoRes.thumb.video_id;
            ret.title = thumbInfoRes.thumb.title;
            ret.thumbnailUrl = thumbInfoRes.thumb.thumbnail_url;
            ret.postTime = DateTime.Parse(thumbInfoRes.thumb.first_retrieve);
            ret.length = NicoApi.convertTimeSpan(thumbInfoRes.thumb.length);
            ret.viewCounter = thumbInfoRes.thumb.view_counter;
            ret.commentNum = thumbInfoRes.thumb.comment_num;
            ret.mylistCounter = thumbInfoRes.thumb.mylist_counter;
            ret.createTime = createTime;

            return ret;
        }



    }
}
