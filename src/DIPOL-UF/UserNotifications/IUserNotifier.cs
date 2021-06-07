namespace DIPOL_UF.UserNotifications
{

    internal enum YesNoResult : byte
    {
        Yes = 1,
        No = 0
    }
    
    internal interface IUserNotifier
    {
        YesNoResult YesNo(string caption, string message);
        YesNoResult YesNoWarning(string caption, string message);
        void Warning(string caption, string message);
        void Error(string caption, string message);
        void Info(string caption, string message);
    }
}