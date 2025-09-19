using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DakLakCoffeeSupplyChain.Common
{
    public class Const
    {
        #region Error Codes

        public static int ERROR_EXCEPTION = -4;
        public static int ERROR_VALIDATION_CODE = -2;
        

        #endregion

        #region Success Codes

        public static int SUCCESS_CREATE_CODE = 1;
        public static string SUCCESS_CREATE_MSG = "Save data success";
        public static int SUCCESS_READ_CODE = 1;
        public static string SUCCESS_READ_MSG = "Get data success";
        public static int SUCCESS_UPDATE_CODE = 1;
        public static string SUCCESS_UPDATE_MSG = "Update data success";
        public static int SUCCESS_DELETE_CODE = 1;
        public static string SUCCESS_DELETE_MSG = "Delete data success";
        public static int SUCCESS_LOGIN_CODE = 1;
        public static string SUCCESS_LOGIN_GOOGLE_MSG = "Login Google success";


        #endregion

        #region Fail code

        public static int FAIL_CREATE_CODE = -1;
        public static string FAIL_CREATE_MSG = "Save data fail";
        public static int FAIL_READ_CODE = -1;
        public static string FAIL_READ_MSG = "Get data fail";
        public static int FAIL_UPDATE_CODE = -1;
        public static string FAIL_UPDATE_MSG = "Update data fail";
        public static int FAIL_DELETE_CODE = -1;
        public static string FAIL_DELETE_MSG = "Delete data fail";

        #endregion

        #region Warning Code

        public static int WARNING_NO_DATA_CODE = 4;
        public static string WARNING_NO_DATA_MSG = "No data";

        #endregion

        #region OTP & Password Reset Codes

        public static int SUCCESS_SEND_OTP_CODE = 1;
        public static string SUCCESS_SEND_OTP_MSG = "OTP sent successfully";
        public static int SUCCESS_VERIFY_OTP_CODE = 1;
        public static string SUCCESS_VERIFY_OTP_MSG = "OTP verified successfully";
        public static int FAIL_VERIFY_OTP_CODE = -1;
        public static string FAIL_VERIFY_OTP_MSG = "OTP verification failed";
        public static int SUCCESS_RESET_PASSWORD_CODE = 1;
        public static string SUCCESS_RESET_PASSWORD_MSG = "Password reset successfully";
        public static int FAIL_RESET_PASSWORD_CODE = -1;
        public static string FAIL_RESET_PASSWORD_MSG = "Password reset failed";

        #endregion

    }
}
