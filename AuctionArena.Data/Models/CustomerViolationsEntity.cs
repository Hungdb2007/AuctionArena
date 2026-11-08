using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AuctionArena.Data.Models
{
    [Table("CustomerViolations", Schema = "dbo")]
    public class CustomerViolationEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ViolationId { get; set; }
        public int CustomerId { get; set; }
        public int SessionId { get; set; }
        public int? LotId { get; set; }
        [StringLength(24)]
        public string ViolationType { get; set; }
        [StringLength(300)]
        public string Description { get; set; }
        public DateTime OccurredAt { get; set; }
    }
}
