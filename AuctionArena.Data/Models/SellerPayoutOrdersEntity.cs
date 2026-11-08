using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace AuctionArena.Data.Models
{
    // Lệnh chi không phải phiếu đã chi. Chỉ chuyển Paid sau khi xác nhận thực chi.
    [Table("SellerPayoutOrders", Schema = "dbo")]
    public class SellerPayoutOrderEntity
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int OrderId {get; set; }
        public int LotId {get; set; }
        public int ConsignorId {get; set; }
        [Column(TypeName = "decimal")]public decimal HammerPrice {get; set; }
        [Column(TypeName = "decimal")]public decimal SellerFeeRate {get; set; }
        [Column(TypeName = "decimal")]public decimal SellerFee {get; set; }
        [Column(TypeName = "decimal")]public decimal PayoutAmount {get; set; }
        [StringLength(16)]public string OrderStatus {get; set; }
        public DateTime CreatedAt {get; set; }
        public int CreatedByUserId {get; set; }
        public DateTime? PaidAt {get; set; }
    }
}
