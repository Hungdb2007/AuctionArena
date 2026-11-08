using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AuctionArena.Data.Models
{
    [Table("AuctionLots", Schema = "dbo")]
    public class AuctionLotEntity
    {
        // Lô đấu giá thuộc một phiên và tham chiếu một hiện vật; lưu giá khởi điểm, bước giá, giá hiện tại, người dẫn đầu/trúng, giá búa và RowVersion.  
        // Thuộc tính và dòng khai báo
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int LotId { get; set; } // thuộc tính
        public int SessionId { get; set; }
        public int ItemId { get; set; }
        public int LotNumber { get; set; }
        [Column(TypeName = "decimal")] 
        public decimal StartingPrice { get; set; }
        [Column(TypeName = "decimal")]
        public decimal ReservePrice { get; set; }
        [Column(TypeName = "decimal")]
        public decimal BidIncrement { get; set; }
        // Mức cọc riêng của lô. NULL ở dữ liệu cũ: dùng quy tắc chung DepositRate.
        [Column(TypeName = "decimal")]
        public decimal? DepositRequired { get; set; }
        [Column(TypeName = "decimal")]
        public decimal CurrentPrice { get; set; }
        public int? LeadingCustomerId { get; set; }
        public int? WinningCustomerId { get; set; }
        [Column(TypeName = "decimal")]
        public decimal? HammerPrice { get; set; }
        [Column(TypeName = "decimal")] public decimal? SellerFeeRateLocked { get; set; }
        public DateTime OpensAt { get; set; }
        public DateTime ClosesAt { get; set; }
        [StringLength(20)]
        public string LotStatus { get; set; }
        [Timestamp]
        public byte[] RowVersion { get; set; }
    }
}
