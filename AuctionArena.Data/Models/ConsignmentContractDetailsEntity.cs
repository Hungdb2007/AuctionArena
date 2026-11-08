using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AuctionArena.Data.Models
{
    [Table("ConsignmentContractDetails", Schema = "dbo")]
    public class ConsignmentContractDetailEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ContractDetailId { get; set; }
        public int ContractId { get; set; }
        public int ItemId { get; set; }
    }
}
