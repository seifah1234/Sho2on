using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HR_Application.Classes
{
    public class Insurance
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public static List<Insurance> Insurances()
        {
            return new List<Insurance>() 
            {
                new Insurance {Id = 0, Name="لا تأمين"},
                new Insurance {Id = 1, Name="اجتماعي"},
                new Insurance {Id = 2, Name="طبي"},
                new Insurance {Id = 3, Name="اجتماعي و طبي"},
            };
        }

        public static string InsuranceName(int id)
        {
            switch (id)
            {
                case 0:
                    return "لا تأمين";
                case 1:
                    return "اجتماعي";
                case 2:
                    return "طبي";
                case 3:
                    return "اجتماعي و طبي";
                default:
                    return "غير معروف";

            }
        }
    }

    
}
