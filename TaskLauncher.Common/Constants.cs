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

    /// <summary>
    /// Role v systemu
    /// </summary>
    public static class Roles
    {
        public const string Admin = "admin";
        public const string User = "user";
    }

    /// <summary>
    /// Nazvy autorizacnich pravidel
    /// </summary>
    public static class Policies
    {
        public const string CanCancelAccount = "can-cancel-account";
        public const string EmailNotConfirmed = "email-not-confirmed";
        public const string UserNotRegistered = "not-registered";
        public const string UserPolicy = "user-policy";
        public const string WorkerPolicy = "worker-policy";
        public const string AdminPolicy = "admin-policy";
        public const string CanViewTaskPolicy = "can-view-task-policy";
        public const string CanHaveProfilePolicy = "can-have-profile-policy";
        public const string CanViewGraphsPolicy = "can-view-graphs-policy";
        public const string CanViewConfigPolicy = "can-view-config-policy";
    }

    /// <summary>
    /// Typy claimu
    /// </summary>
    public static class ClaimTypes
    {
        public const string TokenBalance = "token_balance";
        public const string EmailVerified = "email_verified";
        public const string Vip = "https://bp-claims.com/vip";
        public const string Registered = "https://bp-claims.com/registered";
        public const string Picture = "https://bp-claims.com/picture";
    }

    /// <summary>
    /// Auth. schemata
    /// </summary>
    public static class Authorization
    {
        public const string CookieAuth = "Cookies";
        public const string BearerAuth = "Bearer";
    }

    /// <summary>
    /// Auth0 konstanty
    /// </summary>
    public static class Auth0
    {
        public const string DefaultConnection = "Username-Password-Authentication";
    }

}
