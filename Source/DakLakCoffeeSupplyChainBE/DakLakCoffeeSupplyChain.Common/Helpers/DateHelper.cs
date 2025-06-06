using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common.Helpers
{
    public static class DateHelper
    {
        /// <summary>
        /// Tính số tuổi từ ngày sinh đến ngày hiện tại hoặc ngày chỉ định.
        /// </summary>
        public static int CalculateAge(DateOnly dob, DateOnly? today = null)
        {
            var current = today ?? DateOnly.FromDateTime(DateTime.Today);
            int age = current.Year - dob.Year;
            if (dob > current.AddYears(-age)) age--;

            return age;
        }
    }
}
