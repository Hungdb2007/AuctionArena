using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AuctionArena.Data.Models
{
    [Table("InvoiceDetails", Schema = "dbo")]
    public class InvoiceDetailEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int InvoiceDetailId { get; set; }
        public int InvoiceId { get; set; }
        public int LotId { get; set; }
        [Column(TypeName = "decimal")]
        public decimal HammerPrice { get; set; }
    }
}
