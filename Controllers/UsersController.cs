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
using System.Net.Mail;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Net;
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
                <style>
                    body {
                        margin: 0;
                        padding: 0;
                        font-family: Arial, sans-serif;
                        background-color: color(255,255,255);
                        color: #333333;
                    }
                    .email-container {
                        width: 100%;
                        max-width: 600px;
                        margin: 20px auto;
                        background-color: #ffffff;
                        border-radius: 8px;
                        overflow: hidden;
                        box-shadow: 0 0 10px rgba(0, 0, 0, 0.1);
                    }
                    .header {
                        background-color:#fae18f;
                        padding: 20px;
                        color: #424242;
                        text-align: center;
                    }
                    .header h1 {
                        margin: 0;
                    }
                    .content {
                        padding: 20px;
                        text-align: center;
                    }
                    .button {
                        display: inline-block;
                        padding: 12px 20px;
                        font-size: 20px;
                        color: #ffffff;
                        background-color: #ef9436;
                        text-decoration: none;
                        border-radius: 5px;
                        margin-top: 20px;
                    }
                    .footer {
                        background-color: #f4f4f4;
                        padding: 10px;
                        text-align: center;
                        font-size: 12px;
                        color: #666666;
                    }
                </style>
            </head>
            <body>
                <div class="email-container">
                    <div class="header">
                        <h1>Confirm Email</h1>
                    </div>
                    <div class="content">
                        <p>Hi There {0},</p>
                        <p>Thanks for playing penfootball.online. To confirm your email, press the button below. If you did not request this, please ignore this email.</p>
                        <a href="{1}" class="button">Confirm</a>
                    </div>
                </div>
            </body>
            </html>
            """;

        public UsersController(ILogger<UsersController> logger, UserDataContext userDataContext, IOptions<RatingSettings> ratingSettings, TokenKeySettings tokenkeysettings)
        {
            _logger = logger;
            _userDataContext = userDataContext;
            _ratingSettings = ratingSettings;
            _tokenKeySettings = tokenkeysettings;
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
                JoinDate = currentdate, Role = Roles.Player, Rating = _ratingSettings.Value.StartRating};
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
                new Claim(ClaimTypes.Role, Enum.GetName(user.Role.GetType(), user.Role) ?? "")
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
                        joindate = userModel.JoinDate.ToShortDateString()
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

        //게임 서버가 켜질때 JWT 토큰을 제공함
        //서버 전용 API
        [HttpPost("server/findsecret")]
        public async Task<IActionResult> FindJWTSecret([FromBody] LoginModel loginModel)
        {
            if (_userDataContext.Users.FirstOrDefault(model => (model.Name == loginModel.Username)) is UserModel user && user.Role == Roles.Server)
            {

                var result = _passwordHasher.VerifyHashedPassword(null, user.Password, loginModel.Password);
                if (result == PasswordVerificationResult.Success)
                    return Ok(_tokenKeySettings.Secret);
                else
                    return BadRequest("Wrong Password");
            }
            return BadRequest("Server Auth Failed");
        }


        [HttpPost("emailauth")]
        [Authorize]
        [ProducesResponseType(400)]
        [ProducesResponseType(200)]
        public async Task<IActionResult> EmailAuthenticate([FromBody]string emailaddr)
        {
            _logger.LogInformation("Request for Email Authentication...");

            if (string.IsNullOrEmpty(emailaddr) || !emailRegex.IsMatch(emailaddr))
                return BadRequest("Give a valid Email address");

            //이메일로 보낼 string 제작: stateless하게 처리하고 싶으므로 (ID + 발행시각 + 이메일 주소)에다 EmailSecret으로 Sign
            //해당 string을 퀴리에 집어넣은 링크를 이메일로 전송 -> 적절한 페이지로 redirection

            var userIdstring = User.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
            if (!Int32.TryParse(userIdstring, out int userId))
                return BadRequest("Invalid Token");

            var payload = new
            {
                datetime = DateTime.UtcNow,
                id = userId,
                emailaddr
            };

            string payloadJson = JsonSerializer.Serialize(payload);
            var secretKeyBytes = Encoding.UTF8.GetBytes(_tokenKeySettings.EmailSecret);
            var payloadBytes = Encoding.UTF8.GetBytes(payloadJson);

            //signing procedure
            byte[] signatureBytes;
            using (var hmac = new HMACSHA256(secretKeyBytes))
                signatureBytes = hmac.ComputeHash(payloadBytes);
            string encodedPayload = Convert.ToBase64String(payloadBytes);
            string encodedSignature = Convert.ToBase64String(signatureBytes);

            var authstring = $"{encodedPayload}.{encodedSignature}";
            var authlink = $"https://penfootball.online/api/users/confirmemail?token={authstring}";

            if (!(_userDataContext.Users.Find(userId) is UserModel user))
                return BadRequest("UserID not found in database");

            var emailbody = string.Format(emailBodyFormat, user.Name, authlink);

            try
            {
                // Configure the SMTP client
                SmtpClient smtpClient = new SmtpClient("smtp.yourserver.com") // Use your SMTP server here
                {
                    Port = 587, // Change to your port (usually 587 for TLS)
                    Credentials = new NetworkCredential("yourEmail@domain.com", "yourEmailPassword"), // Use your credentials
                    EnableSsl = true, // Enable SSL for secure email sending
                };

                // Create the email message
                MailMessage mailMessage = new MailMessage
                {
                    From = new MailAddress("yourEmail@domain.com"), // Sender email address
                    Subject = "Confirm Your Email",
                    Body = emailbody,
                    IsBodyHtml = true // Set to true if the body is HTML
                };

                // Add recipient
                mailMessage.To.Add(emailaddr);

                // Send the email
                smtpClient.Send(mailMessage);
                _logger.LogInformation($"Authentication email to {user.Name}({userId}) sent successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Error sending email: {ex.Message}");
            }
            return Ok();
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
