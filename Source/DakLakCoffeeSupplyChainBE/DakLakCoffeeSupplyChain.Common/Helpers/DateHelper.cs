using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.Helpers
{
    public static class DateHelper
    {
        // Tính số tuổi từ ngày sinh đến ngày hiện tại hoặc ngày chỉ định.
        public static int CalculateAge(DateOnly dob, DateOnly? today = null)
        {
            var current = today ?? DateOnly.FromDateTime(DateTime.Today);
            int age = current.Year - dob.Year;
            if (dob > current.AddYears(-age)) age--;

            return age;
        }

        // Convert UTC DateTime sang giờ Việt Nam (UTC+7).
        public static DateTime ConvertToVietnamTime(DateTime utcDateTime)
        {
            var vietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

            return TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc),
                vietnamTimeZone
            );
        }

        // Convert UTC sang timezone chỉ định (VD: "Asia/Tokyo", "America/New_York").
        public static DateTime ConvertToTimeZone(DateTime utcDateTime, string timeZoneId)
        {
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);

            return TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc),
                timeZone
            );
        }

        // Trả về giờ hiện tại theo múi giờ Việt Nam.
        public static DateTime NowVietnamTime()
        {
            return ConvertToVietnamTime(DateTime.UtcNow);
        }

        // Trả về giờ hiện tại theo múi giờ Việt Nam và format theo kiểu DateOnly.
        public static DateOnly ParseDateOnlyFormatVietNamCurrentTime()
            => DateOnly.FromDateTime(NowVietnamTime());
    }
}
