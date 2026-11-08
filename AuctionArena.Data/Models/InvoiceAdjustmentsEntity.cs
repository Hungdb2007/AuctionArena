using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace AuctionArena.Data.Models
{
    // Dấu vết đảo cọc đã chuyển vào hóa đơn khi hủy vì bỏ cọc.
    [Table("InvoiceAdjustments", Schema = "dbo")]
    public class InvoiceAdjustmentEntity
    {
        [Key] [DatabaseGenerated(DatabaseGeneratedOption.Identity)] public int AdjustmentId {get; set; }
        [StringLength(30)] public string AdjustmentCode {get; set; }
        public int InvoiceId {get; set; }
        public int RegistrationId {get; set; }
        [StringLength(32)] public string AdjustmentType {get; set; }
        [Column(TypeName = "decimal")] public decimal Amount {get; set; }
        [StringLength(300)] public string Reason {get; set; }
        public int CreatedByUserId {get; set; }
        public DateTime CreatedAt {get; set; }
    }
}
