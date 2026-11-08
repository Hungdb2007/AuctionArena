using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AuctionArena.Data.Models
{
    [Table("StockReceiptDetails", Schema = "dbo")]
    public class StockReceiptDetailEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ReceiptDetailId { get; set; }
        public int ReceiptId { get; set; }
        public int ItemId { get; set; }
        [StringLength(400)] public string ConditionAtReceipt { get; set; }
        [StringLength(100)] public string LocationAtReceipt { get; set; }
    }
}
