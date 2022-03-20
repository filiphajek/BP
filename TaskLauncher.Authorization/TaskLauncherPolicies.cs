namespace TaskLauncher.Authorization;

public static class TaskLauncherPolicies
{
    public const string CanCancelAccount = "can-cancel-account";
    public const string EmailNotConfirmed = "email-not-confirmed";
    public const string UserNotRegistered = "not-registered";
    public const string UserPolicy = "user-policy";
    public const string AdminPolicy = "admin-policy";
    public const string CanViewTaskPolicy = "can-view-task-policy";
    public const string CanHaveProfilePolicy = "can-have-profile-policy";
    public const string CanViewGraphsPolicy = "can-view-graphs-policy";
}
