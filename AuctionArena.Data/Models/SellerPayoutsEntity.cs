using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AuctionArena.Data.Models
{
    [Table("SellerPayouts", Schema = "dbo")]
    public class SellerPayoutEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int PayoutId { get; set; }
        [StringLength(30)]
        public string PayoutCode { get; set; }
        public int LotId { get; set; }
        public int ConsignorId { get; set; }
        [Column(TypeName = "decimal")]
        public decimal HammerPrice { get; set; }
        [Column(TypeName = "decimal")]
        public decimal SellerFeeRate { get; set; }
        [Column(TypeName = "decimal")]
        public decimal SellerFee { get; set; }
        [Column(TypeName = "decimal")]
        public decimal PayoutAmount { get; set; }
        // NULL cho chứng từ lịch sử không ghi phương thức chi; không tự gán Cash gây sai chứng từ.
        [StringLength(16)]
        public string PaymentMethod { get; set; }
        [StringLength(100)] public string TransferReference {get; set; }
        public DateTime PayoutAt { get; set; }
        public int PaidByUserId { get; set; }
    }
}
