namespace TaskLauncher.Common;

public static class Constants
{
    /// <summary>
    /// Konfiguracni konstanty
    /// </summary>
    public static class Configuration
    {
        public const string FileRemovalRoutine = "autofileremove";
        public const string StartTokenBalance = "starttokenbalance";
        public const string TaskTimeout = "tasktimeout";
        public const string VipTaskPrice = "viptaskprice";
        public const string NormalTaskPrice = "normaltaskprice";

        public static bool IsConfigurationValue(string key)
        {
            var tmp = new[] { FileRemovalRoutine , StartTokenBalance, TaskTimeout, VipTaskPrice, NormalTaskPrice };
            return tmp.Contains(key);
        }
    }

    public static class Roles
    {
        public const string Admin = "admin";
        public const string User = "user";
    }

    public static class Policies
    {
        public const string CanCancelAccount = "can-cancel-account";
        public const string EmailNotConfirmed = "email-not-confirmed";
        public const string UserNotRegistered = "not-registered";
        public const string UserPolicy = "user-policy";
        public const string LauncherPolicy = "launcher";
        public const string AdminPolicy = "admin-policy";
        public const string CanViewTaskPolicy = "can-view-task-policy";
        public const string CanHaveProfilePolicy = "can-have-profile-policy";
        public const string CanViewGraphsPolicy = "can-view-graphs-policy";
        public const string CanViewConfigPolicy = "can-view-config-policy";
    }

    public static class ClaimTypes
    {
        public const string TokenBalance = "token_balance";
        public const string EmailVerified = "email_verified";
        public const string Vip = "https://wutshot-test-api.com/vip";
        public const string Registered = "https://wutshot-test-api.com/registered";
        public const string Picture = "https://wutshot-test-api.com/picture";
    }

    public static class Authorization
    {
        public const string CookieAuth = "Cookies";
        public const string BearerAuth = "Bearer";
    }
}
