namespace Tests
{
    internal static class StaticConfigurationProvider
    {
        const string _plateMotorPort = @"COM1";
        const string _retractorMotorPort = @"COM2";

        public static string PlateMotorPort => _plateMotorPort;
        public static string RetractorMotorPort => _retractorMotorPort;
    }
}
