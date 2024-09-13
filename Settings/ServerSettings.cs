namespace PenFootball_Server.Settings
{

    public class ServerSetting
    {
        public string Password { get; set; }
        public string ApiEndpoint { get; set; }
        public List<Dictionary<string, string>> EntrancePolicy { get; set; }
    }
    public class ServerSettings
    {
        public Dictionary<string, ServerSetting> ServerAccounts { get; set; }
    }
}
