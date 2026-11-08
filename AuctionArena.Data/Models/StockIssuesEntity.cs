using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AuctionArena.Data.Models
{
    [Table("StockIssues", Schema = "dbo")]
    public class StockIssueEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IssueId { get; set; }
        [StringLength(30)]
        public string IssueCode { get; set; }
        public int InvoiceId { get; set; }
        public DateTime IssuedAt { get; set; }
        public int IssuedByUserId { get; set; }
        [StringLength(120)]
        public string RecipientName { get; set; }
        [StringLength(30)] public string RecipientDocument {get; set; }
        [StringLength(160)] public string RecipientRelation {get; set; }
        [StringLength(20)] public string DeliveryMethod {get; set; }
        [StringLength(16)] public string WorkflowStatus {get; set; }
        public string DraftPayload {get; set; }
        [StringLength(300)]
        public string Notes { get; set; }
    }
}
