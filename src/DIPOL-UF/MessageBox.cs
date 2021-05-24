#nullable enable

using System.Windows;

namespace DIPOL_UF
{
    internal static class MessageBox
    {
        public static MessageBoxResult Present(
            string caption,
            string message,
            MessageBoxButton button = MessageBoxButton.OK,
            MessageBoxImage icon = MessageBoxImage.None,
            MessageBoxResult defaultResult = MessageBoxResult.None,
            MessageBoxOptions options = MessageBoxOptions.None,
            Window? window = null
        ) => window is { } w
            ? System.Windows.MessageBox.Show(
                owner: w,
                messageBoxText: message,
                caption: caption,
                button: button,
                icon: icon,
                defaultResult: defaultResult,
                options: options
            )
            : System.Windows.MessageBox.Show(
                messageBoxText: message,
                caption: caption,
                button: button,
                icon: icon,
                defaultResult: defaultResult,
                options: options
            );

        public static MessageBoxResult YesNo(
            string caption,
            string message,
            Window? window = null
        ) => Present(caption, message, MessageBoxButton.YesNo, MessageBoxImage.Question, window: window);


        public static void Error(string caption, string message, Window? window = null) =>
            Present(caption, message, MessageBoxButton.OK, MessageBoxImage.Error, window: window);
        
        public static void Info(string caption, string message, Window? window = null) => 
            Present(caption, message, MessageBoxButton.OK, MessageBoxImage.Information, window: window);

    }
}