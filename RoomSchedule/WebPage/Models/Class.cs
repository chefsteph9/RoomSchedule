//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace WebPage.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class Class
    {
        public int SYN { get; set; }
        public string Semester { get; set; }
        public string Section { get; set; }
        public string Title { get; set; }
        public string Professor { get; set; }
        public string Building { get; set; }
        public string Room { get; set; }
        public string Days { get; set; }
        public System.TimeSpan Begin { get; set; }
        public System.TimeSpan End { get; set; }
    
        public virtual Semester Semester1 { get; set; }
    }
}
