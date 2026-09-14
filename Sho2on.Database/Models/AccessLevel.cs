using System;

namespace Sho2on.Database.Models
{
    [Flags]
    public enum AccessLevel
    {
        None = 0,
        View = 1,
        Edit = 2,
        Full = View | Edit
    }
}
