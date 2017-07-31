using System;
using System.Collections.Generic;

namespace Website.Models
{
    public partial class Class
    {
        public int Syn { get; set; }
        public string Semester { get; set; }
        public string Section { get; set; }
        public string Title { get; set; }
        public string Professor { get; set; }
        public string Building { get; set; }
        public string Room { get; set; }
        public string Days { get; set; }
        public TimeSpan Begin { get; set; }
        public TimeSpan End { get; set; }

        public virtual Semester SemesterNavigation { get; set; }
    }
}
