using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace PenFootball_Server.Models
{
    public enum Roles
    {
        Player, Server, Admin
    }
    public class UserModel
    {
        [Key]
        public int ID { get; set; }
        [Required]
        public string Name { get; set; }
        [Required]
        public string Password { get; set; }
        [Required]
        public Roles Role { get;set; }
        [DataType(DataType.Date)]
        public DateTime JoinDate { get; set; }
        public int Rating { get; set; } 
        public string Email { get; set; }
    }
}
