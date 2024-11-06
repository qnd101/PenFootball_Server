using System.ComponentModel.DataAnnotations;

namespace PenFootball_Server.DB
{
    public class RelStatModel
    {
        public int ID1 { get; set; }
        public int ID2 { get; set; }
        public int Win1 { get; set; }
        public int Win2 { get; set; }
        public int Recent { get; set; }
    }
}
