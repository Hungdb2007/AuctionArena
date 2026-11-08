using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AuctionArena.Data.Models
{
    [Table("Roles", Schema = "dbo")]
    public class RoleEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int RoleId { get; set; }
        [StringLength(40)]
        public string RoleCode { get; set; }
        [StringLength(100)]
        public string RoleName { get; set; }
        public bool IsActive { get; set; }
        [StringLength(300)] public string Description {get; set; }
    }
}
