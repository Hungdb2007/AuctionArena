using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AuctionArena.Data.Models
{
    [Table("Users", Schema = "dbo")]
    public class UserEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int UserId { get; set; }
        [StringLength(60)]
        public string Username { get; set; }
        [StringLength(120)]
        public string FullName { get; set; }
        public int RoleId { get; set; }
        [StringLength(64)]
        public string PasswordHash { get; set; }
        [StringLength(32)]
        public string PasswordSalt { get; set; }
        public int HashIterations { get; set; }
        public bool IsActive { get; set; }
        [StringLength(150)] public string Email {get; set; }
        [StringLength(20)] public string Phone {get; set; }
        [StringLength(250)] public string LockReason {get; set; }
        public DateTime? LastLoginAt {get; set; }
        public DateTime? PasswordChangedAt {get; set; }
        public bool MustChangePassword {get; set; }
        public int FailedPasswordAttempts {get; set; }
        public DateTime? LockedUntil {get; set; }
        public DateTime CreatedAt { get; set; }
        [Timestamp]
        public byte[] RowVersion { get; set; }
    }
}
