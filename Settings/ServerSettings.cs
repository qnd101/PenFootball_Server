using System.Reflection;
using System.Text.RegularExpressions;

namespace PenFootball_Server.Settings
{

    public class ServerSetting
    {
        public string Password { get; set; }
        public string ApiEndpoint { get; set; }
        public List<Dictionary<string, string>> EntrancePolicy { get; set; }


        public bool Validate<T>(T obj)
        {
            // Loop through each key-value pair in the regex dictionary
            foreach (var regexDict in EntrancePolicy)
            {
                if (regexDict.All(property =>
                {
                    string propertyName = property.Key;
                    string regexPattern = property.Value;

                    // Use reflection to get the property value from the object
                    PropertyInfo? propInfo = typeof(T).GetProperty(propertyName);

                    if (propInfo == null)
                        return false;

                    string? propertyValue = propInfo.GetValue(obj)?.ToString();

                    // If the property is null or doesn't match the regex, mark it as invalid
                    if (propertyValue == null || !Regex.IsMatch(propertyValue, regexPattern))
                        return false;
                    return true;
                }))
                    return true;
            }
            return false;
        }
    }
    public class ServerSettings
    {
        public Dictionary<string, ServerSetting> ServerAccounts { get; set; }
    }
}
