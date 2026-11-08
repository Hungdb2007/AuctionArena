using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AuctionArena.Data.Models
{
    [Table("ItemImages", Schema = "dbo")]
    public class ItemImageEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ItemImageId { get; set; }
        public int ItemId { get; set; }
        [StringLength(500)]
        public string RelativePath { get; set; }
        public bool IsPrimary { get; set; }
    }
}
