namespace Sho2on.Database.Models;

public class Reason
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public ReasonType ReasonType { get; set; }

}

public enum ReasonType
{
    EndDuty = 1
}
