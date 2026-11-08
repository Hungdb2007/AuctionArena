using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AuctionArena.Data.Models
{
    [Table("AuditLogs", Schema = "dbo")]
    public class AuditLogEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long AuditLogId { get; set; }
        public int? UserId { get; set; }
        [StringLength(70)]
        public string ActionCode { get; set; }
        [StringLength(60)]
        public string EntityName { get; set; }
        [StringLength(60)]
        public string EntityId { get; set; }
        [StringLength(1000)]
        public string Description { get; set; }
        public DateTime OccurredAt { get; set; }
    }
}
