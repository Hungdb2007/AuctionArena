using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AuctionArena.Data.Models
{
    [Table("Registrations", Schema = "dbo")]
    public class RegistrationEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int RegistrationId { get; set; }
        public int SessionId { get; set; }
        public int CustomerId { get; set; }
        public int PaddleNumber { get; set; }
        [Column(TypeName = "decimal")]
        public decimal DepositRequired { get; set; }
        [Column(TypeName = "decimal")]
        public decimal DepositPaid { get; set; }
        [Column(TypeName = "decimal")]
        public decimal DepositApplied { get; set; }
        [Column(TypeName = "decimal")]
        public decimal DepositRefunded { get; set; }
        [Column(TypeName = "decimal")]
        public decimal DepositForfeited { get; set; }
        [StringLength(16)]
        public string RegistrationStatus { get; set; }
        public DateTime RegisteredAt { get; set; }
        [Timestamp]
        public byte[] RowVersion { get; set; }
    }
}
