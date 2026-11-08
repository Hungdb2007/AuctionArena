using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AuctionArena.Data.Models
{
    [Table("Customers", Schema = "dbo")]
    public class CustomerEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CustomerId { get; set; }
        [StringLength(30)]
        public string CustomerCode { get; set; }
        [StringLength(120)]
        public string FullName { get; set; }
        [StringLength(20)]
        public string Phone { get; set; }
        [StringLength(150)]
        public string Email { get; set; }
        [StringLength(30)]
        public string IdentityNumber { get; set; }
        [StringLength(12)]
        public string MemberTier { get; set; }
        [StringLength(250)] public string Address { get; set; }
        [StringLength(400)] public string PhotoPath { get; set; }
        public DateTime? DateOfBirth { get; set; }
        [StringLength(12)] public string Gender { get; set; }
        [StringLength(15)] public string CustomerStatus { get; set; }
        [StringLength(300)] public string BlacklistReason { get; set; }
        [StringLength(500)] public string Notes { get; set; }
        // Ảnh khách được lưu trong database để có thể mở trên máy khác.
        public byte[] PhotoData { get; set; }
        public bool IsBlacklisted { get; set; }
        public bool HasOutstandingDebt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
