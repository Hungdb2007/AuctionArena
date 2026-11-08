using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AuctionArena.Data.Models
{
    [Table("Items", Schema = "dbo")]
    public class ItemEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ItemId { get; set; }
        [StringLength(30)]
        public string ItemCode { get; set; }
        [StringLength(160)]
        public string ItemName { get; set; }
        public int CategoryId { get; set; }
        public int ConsignorId { get; set; }
        [StringLength(1000)]
        public string Description { get; set; }
        [StringLength(300)] public string Origin { get; set; }
        [StringLength(120)] public string Era { get; set; }
        [StringLength(120)] public string Dimensions { get; set; }
        [StringLength(120)] public string Material { get; set; }
        [StringLength(400)] public string ConditionDescription { get; set; }
        [StringLength(100)] public string WarehouseLocation { get; set; }
        [Column(TypeName = "decimal")]
        public decimal EstimatePrice { get; set; }
        [Column(TypeName = "decimal")] public decimal? EstimatePriceHigh { get; set; }
        [Column(TypeName = "decimal")]
        public decimal? AppraisedPrice { get; set; }
        [StringLength(400)] public string AppraisalNotes { get; set; }
        public int? AppraisedByUserId { get; set; }
        public DateTime? AppraisedAt { get; set; }
        [StringLength(24)]
        public string ItemStatus { get; set; }
        public DateTime CreatedAt { get; set; }
        [Timestamp]
        public byte[] RowVersion { get; set; }
    }
}
