using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LocalNicoMyList
{
    public static class DateTimeExt
    {
        // unix epochをDateTimeで表した定数
        public readonly static DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // UnixTimeからローカルのTimeZoneでのDateTimeを取得する。
        public static DateTime fromUnixTime(long unixTime)
        {
            // unix epochからunixTime秒だけ経過した時刻を求める
            DateTime utc = UnixEpoch.AddSeconds(unixTime);
            return TimeZoneInfo.ConvertTimeFromUtc(utc, TimeZoneInfo.Local);
        }

        // ローカルのTimeZoneでのDateTimeからUnixTimeを取得する。
        public static long toUnixTime(this DateTime src)
        {
            DateTime utc = src.ToUniversalTime();

            // unix epochからの経過秒数を求める
            return (long)utc.Subtract(UnixEpoch).TotalSeconds;
        }
    }
}
