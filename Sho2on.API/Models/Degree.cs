using Sho2on.Database.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Degree
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }   // Code

    [Required, StringLength(50)]
    public string Name { get; set; }

    public DateTime? EditedAt { get; set; } = DateTime.Now;

    public ICollection<User> Users { get; set; } = new List<User>();
}
