using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AuctionArena.Data.Models
{
    [Table("Payments", Schema = "dbo")]
    public class PaymentEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int PaymentId { get; set; }
        [StringLength(30)]
        public string PaymentCode { get; set; }
        public int? InvoiceId { get; set; }
        public int? RegistrationId { get; set; }
        [StringLength(20)]
        public string PaymentType { get; set; }
        [Column(TypeName = "decimal")]
        public decimal Amount { get; set; }
        [StringLength(16)]
        public string PaymentMethod { get; set; }
        [StringLength(100)] public string TransactionReference {get; set; }
        public DateTime PaidAt { get; set; }
        public int ReceivedByUserId { get; set; }
        [StringLength(300)]
        public string Notes { get; set; }
    }
}
