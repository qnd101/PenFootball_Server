using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using Microsoft.Extensions.Options;
using PenFootball_Server.DB;
using PenFootball_Server.Models;
using PenFootball_Server.Services;
using PenFootball_Server.Settings;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace PenFootball_Server.Controllers
{
    public record GameResultData(int Player1ID, int Player2ID, int winner, bool wasdeuce, int score1, int score2);

    [Route("api/[controller]")]
    [ApiController]
    public class ServersController : ControllerBase
    {
        ILogger<ServersController> _logger;
        UserDataContext _userDataContext;
        IOptions<RatingSettings> _ratingSettings;
        IOptions<ServerSettings> _serverSettings;
        TokenKeySettings _tokenKeySettings;
        private readonly PasswordHasher<object> _passwordHasher = new PasswordHasher<object>();

        public ServersController(ILogger<ServersController> logger
            , UserDataContext userDataContext
            , IOptions<RatingSettings> ratingsettings
            , IOptions<ServerSettings> serversettings
            , TokenKeySettings tokenKeySettings) 
        { 
            _logger = logger;
            _userDataContext = userDataContext;
            _ratingSettings = ratingsettings;
            _serverSettings = serversettings;
            _tokenKeySettings = tokenKeySettings;
        }
        // POST api/<GameResultController>

        [HttpPost("gameresult")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [Authorize]
        public async Task<IActionResult> Post([FromBody] GameResultData result)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            _logger.LogInformation("Claims are... "+string.Concat(User.Claims.Select((c) => c.Type+", ")));
            if (User.Claims.FirstOrDefault(c => c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role")?.Value is string role )
            {
                _logger.LogInformation(role);
                int.TryParse(User.Claims.FirstOrDefault(c => c.Type == "sub")?.Value, out int sub);
                _logger.LogInformation($"Game Result Posted by {_userDataContext.Users.Find(sub)?.Name}: " +result.ToString());

                var user1 = _userDataContext.Users.Find(result.Player1ID) ?? throw new Exception("Id in result not found");
                var user2 = _userDataContext.Users.Find(result.Player2ID) ?? throw new Exception("Id in result not found");
                var expW = 1 / (Math.Pow(10, (double)(user2.Rating - user1.Rating) / _ratingSettings.Value.StdDiffRating)+1); //Player1의 예상승률
                int realW = result.winner == 1 ? 1 : 0;
                int delta = (int)(_ratingSettings.Value.StdGainRating * (realW - expW)); //Player1이 얻는 점수

                user1.Rating += delta;
                user2.Rating -= delta;

                _logger.LogInformation($"New Ratings... Player1: {user1.Rating}, Player2: {user2.Rating}");

                if (result.winner == 1)
                {
                    user1.Wins += 1;
                    user2.Loses += 1;
                    if(result.wasdeuce)
                    {
                        user1.Wins99 += 1;
                        user2.Loses99 += 1;
                    }
                }
                else
                {
                    user1.Loses += 1;
                    user2.Wins += 1;
                    if(result.wasdeuce)
                    {
                        user1.Loses99 += 1;
                        user2.Wins99 += 1;
                    }
                }
                user1.SocialCredit += result.score1;
                user2.SocialCredit += result.score2;

                bool exchanged = result.Player1ID > result.Player2ID;
                bool foundmodel= false;
                RelStatModel relstat;
                var exid1 = exchanged ? result.Player2ID : result.Player1ID;
                var exid2 = exchanged ? result.Player1ID : result.Player2ID;
                if (_userDataContext.RelStats.Find(exid1,exid2) is RelStatModel relstatfound)
                {
                    relstat = relstatfound;
                    foundmodel = true;
                }
                else
                    relstat = new RelStatModel() {ID1=exid1, ID2 = exid2 };

                if (result.winner == 1 ^ exchanged)
                    relstat.Win1 += 1;
                else
                    relstat.Win2 += 1;

                relstat.Recent = ((relstat.Recent & 0x7FFFFFFF) << 1) + ((result.winner == 1 ^ exchanged) ? 1 : 0); //1이 이겼으면 1, 2가 이겼으면 0

                if (!foundmodel)
                    _userDataContext.RelStats.Add(relstat);
                
                _userDataContext.SaveChanges();

                return Ok();
            }
            else
            {
                _logger.LogInformation("Client Forbidden trying to post game results");
                return BadRequest("Only Servers can Post Game Results!");
            }    
        }

        //게임 서버가 켜질때 JWT 토큰을 제공함 & 서버의 입장 Policy를 제공
        //서버 전용 API
        [HttpPost("initialize")]
        public async Task<IActionResult> ServerInitData([FromBody] LoginModel loginModel)
        {
            if (_userDataContext.Users.FirstOrDefault(model => (model.Name == loginModel.Username)) is UserModel user && user.Role == Roles.Server)
            {
                var result = _passwordHasher.VerifyHashedPassword(null, user.Password, loginModel.Password);
                if (result != PasswordVerificationResult.Success)
                    return BadRequest("Wrong Password");

                if (!_serverSettings.Value.ServerAccounts.TryGetValue(user.Name, out ServerSetting setting))
                    return BadRequest("Unrecognized Server Name");

                return Ok(new { Secret = _tokenKeySettings.Secret, EntrancePolicy = setting.EntrancePolicy});
            }
            return BadRequest("Server Auth Failed");
        }

    }
}
