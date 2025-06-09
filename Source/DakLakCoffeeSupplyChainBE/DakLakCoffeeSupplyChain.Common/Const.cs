using System;

namespace DakLakCoffeeSupplyChain.Common
{
    public static class Const
    {
        #region Error Codes (500)

        public static int ERROR_EXCEPTION = 500;
        public static string ERROR_EXCEPTION_MSG = "Hệ thống gặp lỗi.";

        public static int ERROR_VALIDATION_CODE = -2;
        public static string ERROR_VALIDATION_MSG = "Lỗi kiểm tra hợp lệ.";

        #endregion

        #region Success Codes (200 ~ 201)

        public static int SUCCESS_CREATE_CODE = 201;
        public static string SUCCESS_CREATE_MSG = "Tạo dữ liệu thành công.";

        public static int SUCCESS_READ_CODE = 200;
        public static string SUCCESS_READ_MSG = "Đọc dữ liệu thành công.";

        public static int SUCCESS_UPDATE_CODE = 200;
        public static string SUCCESS_UPDATE_MSG = "Cập nhật dữ liệu thành công.";

        public static int SUCCESS_DELETE_CODE = 200;
        public static string SUCCESS_DELETE_MSG = "Xóa dữ liệu thành công.";

        public static int SUCCESS_LOGIN_CODE = 200;
        public static string SUCCESS_LOGIN_MSG = "Đăng nhập thành công.";

        public static int SUCCESS_AUTH_CODE = 200;
        public static string SUCCESS_AUTH_MSG = "Xác thực thành công.";

        #endregion

        #region Fail Codes (400 ~ 409)

        public static int FAIL_CREATE_CODE = 409;
        public static string FAIL_CREATE_MSG = "Tạo dữ liệu thất bại.";

        public static int FAIL_READ_CODE = 400;
        public static string FAIL_READ_MSG = "Không thể đọc dữ liệu.";

        public static int FAIL_UPDATE_CODE = 400;
        public static string FAIL_UPDATE_MSG = "Không thể cập nhật dữ liệu.";

        public static int FAIL_DELETE_CODE = 400;
        public static string FAIL_DELETE_MSG = "Không thể xóa dữ liệu.";

        public static int FAIL_VALIDATE_CODE = 400;
        public static string FAIL_VALIDATE_MSG = "Dữ liệu không hợp lệ.";

        public static int FAIL_AUTH_CODE = 401;
        public static string FAIL_AUTH_MSG = "Xác thực thất bại.";

        #endregion

        #region Warning Codes

        public static int WARNING_NO_DATA_CODE = 404;
        public static string WARNING_NO_DATA_MSG = "Không có dữ liệu.";

        #endregion

        #region OTP & Password Reset Codes

        public static int SUCCESS_SEND_OTP_CODE = 200;
        public static string SUCCESS_SEND_OTP_MSG = "Gửi mã OTP thành công.";

        public static int SUCCESS_VERIFY_OTP_CODE = 200;
        public static string SUCCESS_VERIFY_OTP_MSG = "Xác minh OTP thành công.";

        public static int FAIL_VERIFY_OTP_CODE = 400;
        public static string FAIL_VERIFY_OTP_MSG = "Xác minh OTP thất bại.";

        public static int SUCCESS_RESET_PASSWORD_CODE = 200;
        public static string SUCCESS_RESET_PASSWORD_MSG = "Đặt lại mật khẩu thành công.";

        public static int FAIL_RESET_PASSWORD_CODE = 400;
        public static string FAIL_RESET_PASSWORD_MSG = "Đặt lại mật khẩu thất bại.";

        #endregion
    }
}
