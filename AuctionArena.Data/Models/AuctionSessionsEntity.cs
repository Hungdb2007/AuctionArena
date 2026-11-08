using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AuctionArena.Data.Models
{
    [Table("AuctionSessions", Schema = "dbo")]
    public class AuctionSessionEntity
    {
        //Phiên đấu giá có mã, thời gian bắt đầu/kết thúc, trạng thái và người phê duyệt
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int SessionId { get; set; } // thuộc tính
        [StringLength(30)]
        public string SessionCode { get; set; }
        [StringLength(160)]
        public string SessionName { get; set; }
        public DateTime StartsAt { get; set; }
        public DateTime EndsAt { get; set; }
        [StringLength(16)]
        public string SessionStatus { get; set; }
        public int? ApprovedByUserId { get; set; }
    }
}
