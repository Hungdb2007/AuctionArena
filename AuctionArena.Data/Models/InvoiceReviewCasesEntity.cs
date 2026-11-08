using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace AuctionArena.Data.Models
{
    // Phiếu theo dõi/đối soát công nợ quá hạn đã thu một phần, không phải chứng từ đảo tiền.
    [Table("InvoiceReviewCases", Schema = "dbo")]
    public class InvoiceReviewCaseEntity
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]public int ReviewId {get; set; }
        public int InvoiceId {get; set; }
        [Column(TypeName = "decimal")]public decimal RemainingAtReview {get; set; }
        [StringLength(300)]public string ReviewReason {get; set; }
        [StringLength(16)]public string ReviewStatus {get; set; }
        public DateTime CreatedAt {get; set; }
        public int CreatedByUserId {get; set; }
        public DateTime? ResolvedAt {get; set; }
    }
}
