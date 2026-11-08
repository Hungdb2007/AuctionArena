using System;
namespace AuctionArena.Services
{
    public sealed class CustomerParticipation
    {
        public int Registrations
        {
            get;
            set;
        }
        public int WonLots
        {
            get;
            set;
        }
        public int Invoices
        {
            get;
            set;
        }
        public decimal TotalSpent
        {
            get;
            set;
        }
        public decimal RemainingDebt
        {
            get;
            set;
        }
        public decimal HeldDeposit
        {
            get;
            set;
        }
    }
    public sealed class ConsignorPortfolio
    {
        public int Contracts
        {
            get;
            set;
        }
        public int Items
        {
            get;
            set;
        }
        public decimal Paid
        {
            get;
            set;
        }
        public decimal Outstanding
        {
            get;
            set;
        }
    }
    public sealed class ItemJourneyRow
    {
        public DateTime At
        {
            get;
            set;
        }
        public string Phase
        {
            get;
            set;
        }
        public string Detail
        {
            get;
            set;
        }
    }
    public sealed class ConsignorContractRow
    {
        public string Code
        {
            get;
            set;
        }
        public DateTime SignedAt
        {
            get;
            set;
        }
        public string Status
        {
            get;
            set;
        }
    }
    public sealed class ConsignorItemRow
    {
        public string Code
        {
            get;
            set;
        }
        public string Name
        {
            get;
            set;
        }
        public string Status
        {
            get;
            set;
        }
        public decimal Estimated
        {
            get;
            set;
        }
    }
    public sealed class ConsignorPayoutRow
    {
        public string Code
        {
            get;
            set;
        }
        public DateTime At
        {
            get;
            set;
        }
        public decimal Amount
        {
            get;
            set;
        }
    }
    public sealed class CustomerParticipationRow
    {
        public string Code
        {
            get;
            set;
        }
        public DateTime When
        {
            get;
            set;
        }
        public string Type
        {
            get;
            set;
        }
        public decimal Amount
        {
            get;
            set;
        }
        public string Status
        {
            get;
            set;
        }
    }
}
