using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AuctionArena.Data.Models
{
    [Table("SystemSettings", Schema = "dbo")]
    public class SystemSettingEntity
    {
        [Key]
        [StringLength(60)]
        public string SettingKey { get; set; }
        [StringLength(200)]
        public string SettingValue { get; set; }
        [StringLength(300)]
        public string Description { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
