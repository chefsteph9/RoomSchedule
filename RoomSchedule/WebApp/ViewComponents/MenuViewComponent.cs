using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebApp.Models;

namespace WebApp.ViewComponents
{
    public class MenuViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(string semester)
        {
            List<string> param = new List<string>();
            if (semester != null)
                param.Add(semester);
            return View(model: semester);
        }
    }
}
