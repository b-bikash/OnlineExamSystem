using System;

namespace OnlineExamSystem.Helpers
{
    public static class TimeHelper
    {
        public static DateTime GetLocalTime()
        {
            try
            {
                var indiaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
                return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, indiaTimeZone);
            }
            catch (TimeZoneNotFoundException)
            {
                try 
                {
                    var asiaKolkataTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata");
                    return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, asiaKolkataTimeZone);
                }
                catch (TimeZoneNotFoundException)
                {
                    // Fallback to local time if neither are found
                    return DateTime.Now;
                }
            }
        }
    }
}
