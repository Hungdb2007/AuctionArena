using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AuctionArena.Data.Models
{
    [Table("StockIssueDetails", Schema = "dbo")]
    public class StockIssueDetailEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IssueDetailId { get; set; }
        public int IssueId { get; set; }
        public int ItemId { get; set; }
        [StringLength(400)] public string ConditionAtDelivery {get; set; }
        [StringLength(100)] public string LocationAtIssue {get; set; }
    }
}
