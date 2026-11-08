using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace AuctionArena.Data.Models
{
    // Biên bản trả một hiện vật không bán được cho chủ hàng.
    [Table("ConsignorReturns", Schema = "dbo")]
    public class ConsignorReturnEntity
    {
        [Key] [DatabaseGenerated(DatabaseGeneratedOption.Identity)] public int ReturnId { get; set; }
        [StringLength(30)] public string ReturnCode { get; set; }
        public int LotId { get; set; }
        public int ItemId { get; set; }
        public int ConsignorId { get; set; }
        [StringLength(120)] public string RecipientName { get; set; }
        [StringLength(300)] public string Notes { get; set; }
        public DateTime ReturnedAt { get; set; }
        public int ReturnedByUserId { get; set; }
    }
}
