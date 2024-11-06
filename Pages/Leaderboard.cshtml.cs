using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PenFootball_Server.DB;
using PenFootball_Server.Models;
using PenFootball_Server.Settings;
using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace PenFootball_Server.Pages
{
    public class LeaderboardModel : PageModel
    {
        private UserDataContext _context;
        private IOptions<RatingSettings> _ratingSettings;
        private IOptions<ServerSettings> _serverSettings;
        private int _displaycnt = 7;

        public UserModel TopPlayer { get; set; }
        public string TopPlayerRank { get; set; }
        public List<(UserModel, int rank, string rankl)> LowerPlayers { get; set; } = new List<(UserModel, int, string)>();
        public UserModel SearchedPlayer { get; set; }
        public string StateEndPoint { get; set; }

        [BindProperty(SupportsGet = true)]
        public string Name { get; set; }

        [BindProperty(SupportsGet = true)]
        public string Server { get; set; } = "SSHSPhys";

        public LeaderboardModel(UserDataContext context, IOptions<RatingSettings> ratingSettings, IOptions<ServerSettings> serversettings)
        {
            _context = context;
            _ratingSettings = ratingSettings;
            _serverSettings = serversettings;
        }

        private string findRankLetter(int rating)
        {
            return _ratingSettings.Value.RankingThresh
                .Where(item => (item.Key > rating))
                .MinBy(item => item.Key)
                .Value;
        }
        public async Task OnGetAsync()
        {
            var settings = _serverSettings.Value.ServerAccounts[Server];
            StateEndPoint = settings.ApiEndpoint + "/gamedata/players/state";

            var orderedPlayersnRank = _context.Users
                .Where(usr => usr.Role == Roles.Player)
                .OrderByDescending(usr => usr.Rating)
                .ToList()
                .Where(usr => settings.Validate(usr))
                .Select((usr, i) => (usr, i + 1));
            
            var topPlayers= orderedPlayersnRank
                .Take(_displaycnt)
                .ToList();

            if (!topPlayers.Any())
                return;

            TopPlayer = topPlayers.First().Item1;
            TopPlayerRank = findRankLetter(TopPlayer.Rating);

            // Handle search functionality
            if (!string.IsNullOrEmpty(Name))
            {
                var searchresult = orderedPlayersnRank
                    .Where(p => p.usr.Name.ToLower().Contains(Name.ToLower()))
                    .FirstOrDefault();

                if (searchresult is (UserModel player, int rank))
                {
                    // Get the players around the searched player
                    LowerPlayers = orderedPlayersnRank
                        .OrderBy(p => Math.Abs(p.Item2 - rank))
                        .Take(_displaycnt)
                        .OrderBy(p => p.Item2)
                        .Select(p => (p.Item1, p.Item2, findRankLetter(p.Item1.Rating)))
                        .ToList();
                    SearchedPlayer = player;
                    return;
                }
            }
            //When search fails
            LowerPlayers = topPlayers.Skip(1).Select((v) => (v.Item1, v.Item2, findRankLetter(v.Item1.Rating))).ToList();
        }
    }
}
