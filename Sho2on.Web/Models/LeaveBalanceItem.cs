namespace Sho2on.Web.Models;

public class LeaveBalanceItem
{
    public int LeaveTypeId { get; set; }
    public string LeaveTypeName { get; set; } = "";
    public string LeaveTypeCode { get; set; } = "";

    public int TotalBalance { get; set; }
    public int UsedBalance { get; set; }

    public int RemainingBalance => TotalBalance - UsedBalance;
}