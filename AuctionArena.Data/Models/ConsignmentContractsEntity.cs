using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AuctionArena.Data.Models
{
    [Table("ConsignmentContracts", Schema = "dbo")]
    public class ConsignmentContractEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ContractId { get; set; }
        [StringLength(30)]
        public string ContractCode { get; set; }
        public int ConsignorId { get; set; }
        public DateTime SignedAt { get; set; }
        [StringLength(16)]
        public string ContractStatus { get; set; }
        [StringLength(500)]
        public string Notes { get; set; }
        [Column(TypeName = "decimal")] public decimal? SellerFeeRate { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }
}
