namespace Sho2on.API.Dtos
{
    public class LeaveBalanceDto
    {
        public int LeaveTypeId { get; set; }
        public string LeaveTypeName { get; set; }
        public int TotalBalance { get; set; }
        public int UsedBalance { get; set; }
        public int RemainingBalance { get; set; }
        public double PercentageUsed { get; set; }
    }

}
