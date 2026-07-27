namespace Sho2on.Web.Constants
{
    public static class StaticLookups
    {
        public static readonly List<(int Id, string Name)> MaritalStatuses = new()
        {
            (1, "أعزب/عزباء"),
            (2, "متزوج/ة"),
            (3, "مطلق/ة"),
            (4, "أرمل/ة"),
        };

        public static readonly List<(int Id, string Name)> ResidenceTypes = new()
        {
            (1, "مقيم"),
            (2, "مغترب"),
        };

        public static readonly List<(int Id, string Name)> InsuranceTypes = new()
        {
            (0, "لا تأمين"),
            (1, "اجتماعي"),
            (2, "طبي"),
            (3, "اجتماعي و طبي"),
        };
    }
}