using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace AuctionArena.Data.Models
{
    [Table("CustomerTierHistory", Schema = "dbo")]
    public class CustomerTierHistoryEntity
    {
        [Key][DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int HistoryId {get; set; }
        public int CustomerId {get; set; }
        [StringLength(12)] public string OldTier {get; set; }
        [StringLength(12)] public string NewTier {get; set; }
        public int ChangedByUserId {get; set; }
        public DateTime ChangedAt {get; set; }
    }
}
