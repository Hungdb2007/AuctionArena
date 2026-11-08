using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AuctionArena.Data.Models
{
    [Table("Bids", Schema = "dbo")]
    public class BidEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long BidId { get; set; }
        public int LotId { get; set; }
        public int CustomerId { get; set; }
        public int RegistrationId { get; set; }
        [Column(TypeName = "decimal")]
        public decimal Amount { get; set; }
        public DateTime BidAt { get; set; }
        public int EnteredByUserId { get; set; }
    }
}
