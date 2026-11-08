using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AuctionArena.Data.Models
{
    [Table("RolePermissions", Schema = "dbo")]
    public class RolePermissionEntity
    {
        [Key, Column(Order = 0)]
        public int RoleId { get; set; }
        [Key, Column(Order = 1)]
        public int PermissionId { get; set; }
        public bool CanView { get; set; }
        public bool CanCreate { get; set; }
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
        public bool CanPrint { get; set; }
    }
}
