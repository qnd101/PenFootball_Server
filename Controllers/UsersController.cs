using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PenFootball_Server.DB;
using PenFootball_Server.Models;
using PenFootball_Server.Services;
using PenFootball_Server.Settings;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Diagnostics.Eventing.Reader;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Net;
using System.Buffers.Text;
using System.Web;
// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace PenFootball_Server.Controllers
{
    public class LoginModel
    {
        public string Username { get; set; }
        public string Password { get; set; }

        public LoginModel(string username, string password)
        {
            Username = username; Password = password;
        }
    }

    public class UserDataModel
    {
        public string name { get; set; }
        public int rating { get; set; }
        public string rankletter { get; set; }
        public string joindate { get; set; }
        public string email { get; set; }
        public int id { get; set; }
    }

    public class SignupModel
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class UserViewModel
    {
        public string name { get; set; }
        public int rating { get; set; }
        public string rankletter { get; set; }
    }

    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        ILogger<UsersController> _logger;
        UserDataContext _userDataContext;
        IOptions<RatingSettings> _ratingSettings;
        TokenKeySettings _tokenKeySettings;
        IOptions<EmailSettings> _emailSettings;

        private readonly PasswordHasher<object> _passwordHasher = new PasswordHasher<object>();
        private const int ExpiryDurationInMinutes = 60;
        private const int minpasswordlen = 4;
        private const int maxusernamelen = 10;

        private static Regex emailRegex = new Regex(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

        private static string emailBodyFormat = """
            <!DOCTYPE html>
            <html lang="en">
            <head>
                <meta charset="UTF-8">
                <meta name="viewport" content="width=device-width, initial-scale=1.0">
                <title>Confirm Your Email</title>
            </head>
            <body style="margin: 0; padding: 0; font-family: Arial, sans-serif; background-color: rgb(255,255,255); color: #333333;">
                <div class="email-container" style="width: 100%; max-width: 600px; margin: 20px auto; background-color: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 0 10px rgba(0, 0, 0, 0.1);">
                    <div class="header" style="background-color:#fae18f; padding: 20px; color: #424242; text-align: center;">
                        <h1 style="margin: 0;">Confirm Email</h1>
                    </div>
                    <div class="content" style="padding: 20px; text-align: center;">
                        <p>Hi There {0},</p>
                        <p>Thanks for playing penfootball.online. To confirm your email, press the button below. If you did not request this, please ignore this email.</p>
                        <a href="{1}" class="button" style="display: inline-block; padding: 12px 20px; font-size: 20px; color: #ffffff; background-color: #ef9436; text-decoration: none; border-radius: 5px; margin-top: 20px;">Confirm</a>
                    </div>
                    <div class="footer" style="background-color: #f4f4f4; padding: 10px; text-align: center; font-size: 12px; color: #666666;">
                        <p>© 2024 Pen Football Online</p>
                    </div>
                </div>
            </body>
            </html>
            """;

        private static string emailConfirmedFormat = """
            <!DOCTYPE html>
            <html lang="en">
            <head>
                <meta charset="UTF-8">
                <meta name="viewport" content="width=device-width, initial-scale=1.0">
                <title>Email Confirmed</title>
            </head>
            <body>
            <h1>Your Email is Confirmed</h1>
            <a>{0} is added to {1}'s account</a>
            </body>
            """;

        public UsersController(ILogger<UsersController> logger
            , UserDataContext userDataContext
            , IOptions<RatingSettings> ratingSettings
            , IOptions<EmailSettings> emailSettings
            , TokenKeySettings tokenkeysettings)
        {
            _logger = logger;
            _userDataContext = userDataContext;
            _ratingSettings = ratingSettings;
            _tokenKeySettings = tokenkeysettings;
            _emailSettings = emailSettings;
        }

        [HttpPost("signup")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Signup([FromBody] SignupModel signupModel)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            _logger.LogInformation($"new signup event by username: {signupModel.Username}");
            if (_userDataContext.Users.Any(item => item.Name == signupModel.Username))
                return BadRequest("Username already exists!");

            if (signupModel.Password.Length < minpasswordlen)
                return BadRequest($"Password should be at least {minpasswordlen} characters");

            if (signupModel.Username.Length > maxusernamelen)
                return BadRequest($"Username should be at most {maxusernamelen} characters");

            if (!Regex.IsMatch(signupModel.Username, "^[a-zA-Z0-9가-힣]+$"))
                return BadRequest($"Username can only contain number, alphabets, or hangeul characters");

            var currentdate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day);
            var hashedpassword = _passwordHasher.HashPassword(null, signupModel.Password);
            var newuser = new UserModel() { Name = signupModel.Username, Password = hashedpassword, 
                JoinDate = currentdate, Role = Roles.Player, Rating = _ratingSettings.Value.StartRating, Email=""};
            _userDataContext.Users.Add(newuser);
            _userDataContext.SaveChanges();
            _logger.LogInformation($"Successfully Added User {signupModel.Username} to Database");
            return Ok();
        }

        // POST api/users/login
        [HttpPost("login")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Login([FromBody] LoginModel loginModel)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _logger.LogInformation($"username: {loginModel.Username}, password: {loginModel.Password} Login Attempt");

            if (_userDataContext.Users.FirstOrDefault(model => (model.Name == loginModel.Username)) is UserModel user)
            {
                var result = _passwordHasher.VerifyHashedPassword(null, user.Password, loginModel.Password);
                _logger.LogInformation($"Found User from Database. Verifying...");
                _logger.LogInformation($"Result: {result}");
                if (result == PasswordVerificationResult.Success)
                    return Ok(generateToken(user.ID));
                else
                    return BadRequest("Wrong Password");
            }

            _logger.LogInformation($"username {loginModel.Username} not found!!");
            return BadRequest("Username Not Found");
        }

        private string generateToken(int userId)
        {
            var symmetricKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_tokenKeySettings.Secret));
            var credentials = new SigningCredentials(symmetricKey, SecurityAlgorithms.HmacSha256);
            var user = _userDataContext.Users.Find(userId) ?? throw new Exception("user not found");

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Role, Enum.GetName(user.Role.GetType(), user.Role) ?? ""),
                new Claim("email", user.Email.ToString())
            };

            var token = new JwtSecurityToken(
                issuer: "penfootball-server", // Replace with your issuer
                audience: "penfootball-frontend", // Replace with your audience
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(ExpiryDurationInMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        [HttpGet("mydata")]
        [Authorize]
        [ProducesResponseType(400)]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetUserData()
        {
            _logger.LogInformation("Request For User Data...");
            var userIdstring = User.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
            if(Int32.TryParse(userIdstring, out int userId))
            {
                if(_userDataContext.Users.Find(userId) is UserModel userModel)
                {
                    var userdatamodel = new UserDataModel()
                    {
                        name = userModel.Name,
                        rating = userModel.Rating,
                        rankletter = findRankLetter(userModel.Rating),
                        joindate = userModel.JoinDate.ToShortDateString(),
                        email = userModel.Email,
                        id = userModel.ID
                    };
                    return Ok(userdatamodel);
                }
            }
            return BadRequest("Invalid Token");
        }

        private string findRankLetter(int rating)
        {
            return _ratingSettings.Value.RankingThresh
                .Where(item => (item.Key > rating))
                .MinBy(item => item.Key)
                .Value;
        }

        [HttpGet("view")]
        [ProducesResponseType(400)]
        [ProducesResponseType(200)]
        public async Task<IActionResult> ViewUser([FromQuery]int id)
        {
            _logger.LogInformation($"View user info of ID : {id}");
            if (_userDataContext.Users.Find(id) is UserModel userModel)
            {
                var view = new UserViewModel()
                {
                    name = userModel.Name,
                    rating = userModel.Rating,
                    rankletter = findRankLetter(userModel.Rating)
                };
                return Ok(view);
            }
            return BadRequest("No player with ID");
        }

        public record TokenPayload(DateTime expiration, int id, string email);

        [HttpPost("emailauth")]
        [Authorize]
        [ProducesResponseType(400)]
        [ProducesResponseType(200)]
        public async Task<IActionResult> EmailAuthenticate([FromBody]string emailaddr)
        {
            _logger.LogInformation("Request for Email Authentication...");

            if(!ModelState.IsValid)
            {
                _logger.LogInformation($"Model State is not Valid");
                return BadRequest(ModelState);
            }

            if (string.IsNullOrEmpty(_emailSettings.Value.EndPoint))
                return NotFound("Email endpoint not found in server");

            if (string.IsNullOrEmpty(emailaddr) || !emailRegex.IsMatch(emailaddr))
                return BadRequest("Give a valid Email address");

            if (_userDataContext.Users.Any(var => var.Email == emailaddr))
                return BadRequest("You already registered this email or it is already used by one of our users");

            //이메일로 보낼 string 제작: stateless하게 처리하고 싶으므로 (ID + 발행시각 + 이메일 주소)에다 EmailSecret으로 Sign
            //해당 string을 퀴리에 집어넣은 링크를 이메일로 전송 -> 적절한 페이지로 redirection

            var userIdstring = User.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
            if (!Int32.TryParse(userIdstring, out int userId))
                return BadRequest("Invalid Token");

            //10분의 제한시간
            var payload = new TokenPayload(DateTime.UtcNow + TimeSpan.FromMinutes(10), userId, emailaddr);

            string payloadJson = JsonSerializer.Serialize(payload);
            var secretKeyBytes = Encoding.UTF8.GetBytes(_tokenKeySettings.EmailSecret);
            var payloadBytes = Encoding.UTF8.GetBytes(payloadJson);

            //signing procedure
            byte[] signatureBytes;
            using (var hmac = new HMACSHA256(secretKeyBytes))
                signatureBytes = hmac.ComputeHash(payloadBytes);
            string encodedPayload = Convert.ToBase64String(payloadBytes);
            string encodedSignature = Convert.ToBase64String(signatureBytes);

            var authstring = HttpUtility.UrlEncode($"{encodedPayload}.{encodedSignature}");
            var authlink = $"https://penfootball.online/api/users/confirmemail?token={authstring}";

            if (!(_userDataContext.Users.Find(userId) is UserModel user))
                return BadRequest("UserID not found in database");

            var emailbody = string.Format(emailBodyFormat, user.Name, authlink);

            using HttpClient client = new HttpClient();
            try
            {
                // Create the email message
                var mailMessage = new
                {
                    api_key = _emailSettings.Value.Key,
                    sender = "\"Pen Football Online\" <confirm@penfootball.online>", // Sender email address
                    to = new string[] { emailaddr },
                    subject = "Confirm Your Email",
                    html_body = emailbody
                };
                var content = new StringContent(JsonSerializer.Serialize(mailMessage), Encoding.UTF8, "application/json");
                var response = await client.PostAsync(_emailSettings.Value.EndPoint, content);
                // Send the email
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    _logger.LogInformation($"Email sending faliled. Content: {await response.Content.ReadAsStringAsync()}");
                    return BadRequest("Failed to send Email");
                }
                _logger.LogInformation($"Authentication email to {user.Name}({userId}) sent successfully.");

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Error sending email: {ex.Message}");
                return BadRequest("Failed to send Email");
            }
        }

        [HttpGet("confirmemail")]
        public async Task<IActionResult> ConfirmMail(string token)
        {
            _logger.LogInformation($"Confirming Token {token}");

            if (string.IsNullOrEmpty(_emailSettings.Value.EndPoint))
                return NotFound("Email endpoint not found in server");

            if (!ModelState.IsValid)
            {
                _logger.LogInformation("Model Validation Failed");
                return BadRequest("Invalid Token");
            }

            var splittoken = token.Split('.');
            string encodedpayload = splittoken[0], signature = splittoken[1];

            if (!isbase64(encodedpayload) || !isbase64(signature))
            {
                _logger.LogInformation("Non Base64 char contained");
                return BadRequest("Invalid Token");
            }

            //Signing validation
            var sigbytes = Convert.FromBase64String(signature);
            var payloadbytes = Convert.FromBase64String(encodedpayload);
            var secretKeyBytes = Encoding.UTF8.GetBytes(_tokenKeySettings.EmailSecret);

            bool isvalid;
            using (var hmac = new HMACSHA256(secretKeyBytes))
                isvalid = hmac.ComputeHash(payloadbytes).SequenceEqual(sigbytes);

            if(!isvalid)
            {
                _logger.LogInformation("Signature validation failed");
                return BadRequest("Invalid Token");
            }    

            //Check payload
            var payloadnullable = JsonSerializer.Deserialize<TokenPayload>(Encoding.UTF8.GetString(payloadbytes));
            if (!(payloadnullable is TokenPayload payload))
            {
                _logger.LogInformation("Wrong format for token payload");
                return BadRequest("Invalid Token");
            }

            if(payload.expiration < DateTime.UtcNow)
            {
                _logger.LogInformation("Token expired");
                return BadRequest("Token expired");
            }


            if (!(_userDataContext.Users.Find(payload.id) is UserModel userModel))
            {
                _logger.LogInformation("User in token not found");
                return BadRequest("User not found");
            }

            if(!string.IsNullOrEmpty(userModel.Email))
            {
                _logger.LogInformation("User already has email");
                return BadRequest("You already registered an email");
            }

            userModel.Email = payload.email;

            _userDataContext.SaveChanges();

            _logger.LogInformation($"Successfully added email address {payload.email} to user {userModel.Name}({userModel.ID})");

            return new ContentResult
            {
                Content = string.Format(emailConfirmedFormat, userModel.Email, userModel.Name),
                ContentType = "text/html",
                StatusCode = 200
            };
        }

        private bool isbase64(string str)
        {
            // Check if the string contains only Base64 valid characters
            foreach (char c in str)
            {
                if (!(char.IsLetterOrDigit(c) || c == '+' || c == '/' || c == '='))
                {
                    return false;
                }
            }
            return true;
        }

        /*
        // PUT api/<UserLoginController>/5  
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/<UserLoginController>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }*/
    }
}
