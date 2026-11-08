using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace AuctionArena.Data.Models
{
    // Nghĩa vụ hoàn cọc tạo khi đóng phiên, không phải bằng chứng đã chi tiền.
    [Table("DepositRefundObligations", Schema = "dbo")]
    public class DepositRefundObligationEntity
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ObligationId {get; set; }
        public int SessionId {get; set; }
        public int RegistrationId {get; set; }
        [Column(TypeName = "decimal")]public decimal DueAmount {get; set; }
        [StringLength(16)]public string ObligationStatus {get; set; }
        public DateTime CreatedAt {get; set; }
        public DateTime? SettledAt {get; set; }
    }
}
