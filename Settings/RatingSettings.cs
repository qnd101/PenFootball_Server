namespace PenFootball_Server.Settings
{
    public class RatingSettings
    {
        //승률이 10배 차이나는 레이팅 값
        public int StdDiffRating { get; set; }
        //대충 한 경기당 얻는 레이팅 값의 스케일
        public int StdGainRating { get; set; }

        public Dictionary<int, string> RankingThresh { get; set; } 
        public int StartRating { get; set; }
    }
}
