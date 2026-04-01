using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HR_Application.Classes
{
    public class Marital
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public static List<Marital> Maritals()
        {
            return new List<Marital>() 
            {
                new Marital {Id = 1, Name="أعزب/عزباء"},
                new Marital {Id = 2, Name="متزوج/ة"},
                new Marital {Id = 3, Name="مطلق/ة"},
                new Marital {Id = 4, Name="أرمل/ة"},
            };
        }

        public static string MaritalName(int id, bool isMale)
        {
            switch (id)
            {
                case 1:
                    return (isMale) ?  "أعزب" : "عزباء";
                case 2:
                    return (isMale) ? "متزوج" : "متزوجة";
                case 3:
                    return (isMale) ? "مطلق" : "مطلقة";
                case 4:
                    return (isMale) ?  "أرمل" : "أرملة";
                default:
                    return "غير معروف";
            }
        }
    }
}
