using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using Microsoft.Extensions.Options;
using PenFootball_Server.DB;
using PenFootball_Server.Models;
using PenFootball_Server.Settings;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace PenFootball_Server.Controllers
{
    public record GameResultData(int Player1ID, int Player2ID, int winner);

    [Route("api/[controller]")]
    [ApiController]
    public class GameResultController : ControllerBase
    {
        ILogger<GameResultController> _logger;
        UserDataContext _userDataContext;
        IOptions<RatingSettings> _ratingSettings;

        public GameResultController(ILogger<GameResultController> logger, UserDataContext userDataContext, IOptions<RatingSettings> ratingsettings) 
        { 
            _logger = logger;
            _userDataContext = userDataContext;
            _ratingSettings = ratingsettings;
        }
        // POST api/<GameResultController>

        [HttpPost]
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
                _userDataContext.SaveChanges();
               
                return Ok();
            }
            else
            {
                _logger.LogInformation("Client Forbidden trying to post game results");
                return BadRequest("Only Servers can Post Game Results!");
            }    
        }
    }
}
