using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AuctionArena.Data.Models
{
    [Table("StockReceipts", Schema = "dbo")]
    public class StockReceiptEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ReceiptId { get; set; }
        [StringLength(30)]
        public string ReceiptCode { get; set; }
        public int ContractId { get; set; }
        public DateTime ReceivedAt { get; set; }
        public int ReceivedByUserId { get; set; }
        [StringLength(160)] public string DeliveredBy {get; set; }
        [StringLength(300)]
        public string Notes { get; set; }
    }
}
