using System;

namespace Sample.Function.Extensions
{
    public static class DateTimeExtensions
    {
        public static DateTime ToYearMonthDayFormat(this DateTime dateTime)
        {
            return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day);
        }
    }
}
