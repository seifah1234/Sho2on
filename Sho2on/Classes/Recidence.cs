using System; using HR_Application.Helpers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HR_Application.Classes
{
    public class Recidence
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public static List<Recidence> Recidences()
        {
            return new List<Recidence>()
            {
                new Recidence { Id = 1, Name = "مقيم" },
                new Recidence { Id = 2, Name = "مغترب" },
            };
        }

        public static string RecidenceName(int id)
        {
            switch (id)
            {
                case 1:
                    return "مقيم";
                case 2:
                    return "مغترب";
                default:
                    return "غير معروف";
            }
        }
    }

    
}
