using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoinbaseExchange.NET.Utilities
{
    public static class DateTimeUtilities
    {
        private static readonly DateTime UnixEpoch =
            new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static double ToUnixTimestamp(this DateTime dateTime)
        {
            return (dateTime - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
        }

        public static DateTime DateTimeFromUnixTimestampSeconds(string seconds)
        {
            long longLecs = Convert.ToInt64(seconds);
            return UnixEpoch.AddSeconds(longLecs);
        }


    }
}
