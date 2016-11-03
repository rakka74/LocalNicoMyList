using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace LocalNicoMyList.nicoApi
{
    [System.Xml.Serialization.XmlRoot("nicovideo_thumb_response")]
    public class ThumbInfoResponse
    {
        /// <summary>動画情報</summary>
        [XmlElement]
        public Thumb thumb;

        /// <summary>エラーコード</summary>
        [XmlElement]
        public Error error;

        /// <summary>成功か失敗か</summary>
        [XmlAttribute]
        public string status;
    }

    public class Thumb
    {
        /// <summary>動画ID</summary>
        [XmlElement]
        public string video_id;

        /// <summary>タイトル</summary>
        [XmlElement]
        public string title;

        /// <summary>動画説明文</summary>
        [XmlElement]
        public string description;

        /// <summary>サムネイル画像URL</summary>
        [XmlElement]
        public string thumbnail_url;

        /// <summary>投稿日時</summary>
        [XmlElement]
        public string first_retrieve;

        /// <summary>再生時間</summary>
        [XmlElement]
        public string length;

        /// <summary>動画の形式</summary>
        [XmlElement]
        public string movie_type;

        /// <summary>動画サイズ</summary>
        [XmlElement]
        public int size_high;

        /// <summary>エコノミー時の動画サイズ、0である場合はVideoSizeと同じ</summary>
        [XmlElement]
        public int size_low;

        /// <summary>再生数</summary>
        [XmlElement]
        public int view_counter;

        /// <summary>コメント数</summary>
        [XmlElement]
        public int comment_num;

        /// <summary>マイリスト数</summary>
        [XmlElement]
        public int mylist_counter;

        /// <summary>最後に投稿されたコメント文</summary>
        [XmlElement]
        public string last_res_body;

        /// <summary>動画ページのURL</summary>
        [XmlElement]
        public string watch_url;

        /// <summary>調査中</summary>
        [XmlElement]
        public string thumb_type;

        /// <summary>外部再生の可否</summary>
        [XmlElement]
        public bool embeddable;

        /// <summary>ニコ生で再生できないか</summary>
        [XmlElement]
        public bool no_live_play;

        /// <summary>タグリスト</summary>
        [XmlElement]
        public Tags tags;

        /// <summary>投稿者のユーザーID</summary>
        [XmlElement]
        public string user_id;

        /// <summary>投稿者のユーザー名</summary>
        [XmlElement]
        public string user_nickname;

        /// <summary>投稿者のユーザーアイコン</summary>
        [XmlElement]
        public string user_icon_url;
    }

    public class Tags
    {
        /// <summary>言語</summary>
        [XmlElement]
        public string domain;

        /// <summary>タグ</summary>
        [XmlElement]
        public Tag[] tag;
    }

    public class Tag
    {
        /// <summary>タグ名</summary>
        [XmlText]
        public string tag;

        /// <summary>カテゴリ</summary>
        [XmlAttribute]
        public int category;

        /// <summary>タグロック</summary>
        [XmlAttribute("lock")]
        public int _lock; //予約語なので先頭にハイフン
    }
}
