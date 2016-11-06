using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace LocalNicoMyList.nicoApi
{
    public class NicoApi
    {
        public NicoApi()
        {

        }

        public async Task<ThumbInfoResponse> getThumbInfo(string videoId)
        {
            try
            {
                var hc = new HttpClient();
                string url = string.Format("http://ext.nicovideo.jp/api/getthumbinfo/{0}", videoId);
                byte[] ret = await hc.GetByteArrayAsync(url);
                var inStream = new MemoryStream(ret);
                var serialize = new XmlSerializer(typeof(ThumbInfoResponse));
                return (ThumbInfoResponse)serialize.Deserialize(inStream);
            }
            catch(Exception e)
            {
                Console.WriteLine(e);
                return null;
            }
        }

        public class AccessLockedException : Exception
        {
            public AccessLockedException()
            {
            }
        }

        // 最新コメントの日時を取得
        public async Task<DateTime?> getLatestCommentTimeAsync(string videoId, string cookieHeader/*CookieContainer cookieContainer*/)
        {
            string threadId = null;
            string messageServerUrl = null;
            var handler = new HttpClientHandler()
            {
                UseCookies = false
            };
            var hc = new HttpClient(handler);
            hc.DefaultRequestHeaders.Add("Cookie", cookieHeader);
            string url = string.Format("http://flapi.nicovideo.jp/api/getflv/{0}", videoId);
            string ret = await hc.GetStringAsync(url);
            var nameValues = this.parseQueryString(ret);
            threadId = nameValues.Get("thread_id");
            messageServerUrl = nameValues.Get("ms");
            string error = nameValues.Get("error");

            if (null != error && error.Equals("access_locked"))
            {
                throw new AccessLockedException();
            }

            if (null != threadId && null != messageServerUrl)
            {
                hc = new HttpClient();
                url = string.Format("{0}thread?version=20090904&thread={1}&res_from=-1", messageServerUrl, threadId);
                ret = await hc.GetStringAsync(url);

                XmlDocument xdoc = new XmlDocument();
                xdoc.LoadXml(ret);
                XmlElement root = xdoc.DocumentElement;
                XmlNode chat = root?.SelectSingleNode("chat");
                if (null == chat)   // コメントが存在しない場合
                    return DateTimeExt.fromUnixTime(0);
                return DateTimeExt.fromUnixTime(long.Parse(chat.Attributes["date"].Value));
            }

            return null;
        }

        public async Task<NameValueCollection> getflvAsync(string videoId, string cookieHeader)
        {
            var handler = new HttpClientHandler()
            {
                UseCookies = false
            };
            var hc = new HttpClient(handler);
            hc.DefaultRequestHeaders.Add("Cookie", cookieHeader);
            string url = string.Format("http://flapi.nicovideo.jp/api/getflv/{0}", videoId);
            string ret = await hc.GetStringAsync(url);
            return this.parseQueryString(ret);
        }


        NameValueCollection parseQueryString(string query)
        {
            var ret = new NameValueCollection();
            foreach (string pair in query.Split('&'))
            {
                string[] kv = pair.Split('=');

                string key = kv.Length == 1
                  ? null : Uri.UnescapeDataString(kv[0]).Replace('+', ' ');

                string[] values = Uri.UnescapeDataString(
                  kv.Length == 1 ? kv[0] : kv[1]).Replace('+', ' ').Split(',');

                foreach (string value in values)
                {
                    ret.Add(key, value);
                }
            }
            return ret;
        }

        public enum Status
        {
            /// <summary>正常に情報を取得出来ました</summary>
            OK,
            /// <summary>削除済みです</summary>
            Deleted,
            /// <summary>何らかの理由で情報を取得出来ませんでした</summary>
            UnknownError,
        }

        public static Status convertStatus(string Serial, Error ErrorCode)
        {
            switch (Serial)
            {
                case "ok": return Status.OK;
                case "fail":
                    if (ErrorCode == null) break;

                    switch (ErrorCode.code)
                    {
                        case "DELETED": return Status.Deleted;
                    }
                    break;
            }

            return Status.UnknownError;
        }

        public static TimeSpan convertTimeSpan(string Serial)
        {
            string[] buf = Serial.Split(':');
            var minute = int.Parse(buf[0]);

            return new TimeSpan((int)(minute / 60), minute % 60, int.Parse(buf[1]));
        }

    }
}
