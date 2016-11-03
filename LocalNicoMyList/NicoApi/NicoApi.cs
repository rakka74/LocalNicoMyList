using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
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
