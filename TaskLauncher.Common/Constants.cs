namespace TaskLauncher.Common;

public class Constants
{
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
}
