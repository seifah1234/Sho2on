using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sho2on.Database.Models
{
    public class Branch
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";

        public int? AreaId { get; set; }
        public Area? Area { get; set; }

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        public int RadiusMeters { get; set; } = 100;

        public DateTime EditedAt { get; set; }

        public virtual ICollection<UserBranch> UserBranches { get; set; } = new List<UserBranch>();

        public virtual ICollection<User> Users { get; set; } = new List<User>();

    }
}
