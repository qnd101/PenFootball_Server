namespace PenFootball_Server.Services
{
    public class TokenKeySettings
    {
        public string Secret { get; set; }
        public string EmailSecret { get; set; }

        public TokenKeySettings()
        {
            var randombytes = new byte[64];
            using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
            {
                rng.GetBytes(randombytes);
                Secret = Convert.ToBase64String(randombytes);
                rng.GetBytes(randombytes);
                EmailSecret = Convert.ToBase64String(randombytes);
            }
        }
    }
}
