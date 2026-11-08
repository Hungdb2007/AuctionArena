using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AuctionArena.Data.Models
{
    [Table("Permissions", Schema = "dbo")]
    public class PermissionEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int PermissionId { get; set; }
        [StringLength(60)]
        public string PermissionCode { get; set; }
        [StringLength(100)]
        public string PermissionName { get; set; }
    }
}
