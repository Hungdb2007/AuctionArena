using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AuctionArena.Data.Models
{
    [Table("Invoices", Schema = "dbo")]
    public class InvoiceEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int InvoiceId { get; set; }
        [StringLength(30)]
        public string InvoiceCode { get; set; }
        public int SessionId { get; set; }
        public int CustomerId { get; set; }
        public DateTime IssuedAt { get; set; }
        public DateTime DueAt { get; set; }
        [StringLength(16)]
        public string InvoiceStatus { get; set; }
        [Column(TypeName = "decimal")]
        public decimal HammerSubtotal { get; set; }
        [Column(TypeName = "decimal")]
        public decimal BuyerFee { get; set; }
        [Column(TypeName = "decimal")]
        public decimal VipDiscount { get; set; }
        [Column(TypeName = "decimal")]
        public decimal VatAmount { get; set; }
        [Column(TypeName = "decimal")]
        public decimal DepositApplied { get; set; }
        [Column(TypeName = "decimal")]
        public decimal TotalDue { get; set; }
        [Column(TypeName = "decimal")]
        public decimal PaidAmount { get; set; }
        [Column(TypeName = "decimal")]
        public decimal BuyerFeeRate { get; set; }
        [Column(TypeName = "decimal")]
        public decimal VatRate { get; set; }
        [Column(TypeName = "decimal")]
        public decimal VipDiscountRate { get; set; }
        [Timestamp]
        public byte[] RowVersion { get; set; }
    }
}
