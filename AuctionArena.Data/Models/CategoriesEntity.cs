using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AuctionArena.Data.Models
{
    [Table("Categories", Schema = "dbo")]
    public class CategorieEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CategoryId { get; set; }
        [StringLength(100)]
        public string CategoryName { get; set; }
        [StringLength(30)] public string CategoryCode { get; set; }
        [StringLength(500)] public string Description { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; }
    }
}
