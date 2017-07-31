using System;
using System.Collections.Generic;

namespace WebApp.Models
{
    public partial class Semester
    {
        public Semester()
        {
            Class = new HashSet<Class>();
        }

        public string Name { get; set; }
        public bool Default { get; set; }
        public bool Display { get; set; }

        public virtual ICollection<Class> Class { get; set; }
    }
}
