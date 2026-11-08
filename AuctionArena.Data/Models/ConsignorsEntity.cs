using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AuctionArena.Data.Models
{
    [Table("Consignors", Schema = "dbo")]
    public class ConsignorEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ConsignorId { get; set; }
        [StringLength(30)]
        public string ConsignorCode { get; set; }
        [StringLength(120)]
        public string FullName { get; set; }
        [StringLength(20)]
        public string Phone { get; set; }
        [StringLength(30)]
        public string IdentityNumber { get; set; }
        [StringLength(250)]
        public string Address { get; set; }
        [StringLength(20)] public string ConsignorType { get; set; }
        [StringLength(120)] public string RepresentativeName { get; set; }
        [StringLength(150)] public string Email { get; set; }
        [StringLength(120)] public string BankName { get; set; }
        [StringLength(60)] public string BankAccount { get; set; }
        [StringLength(120)] public string BankAccountHolder { get; set; }
        [StringLength(16)] public string ConsignorStatus { get; set; }
        [StringLength(500)] public string Notes { get; set; }
        [Column(TypeName = "decimal")] public decimal? SellerFeeRate { get; set; }
    }
}
