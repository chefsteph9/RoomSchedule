using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using WebApp.Models;

namespace WebApp.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult About()
        {
            ViewData["Message"] = "Your application description page.";

            return View();
        }

        public IActionResult Contact()
        {
            ViewData["Message"] = "Your contact page.";

            return View();
        }

        public IActionResult Error()
        {
            return View();
        }

        [HttpGet]
        public List<Class> GetClasses(string semester, string building, string room)
        {
            List<Class> classes = new List<Class>();
            using (var dbContext = new RoomScheduleContext())
            {
                classes = dbContext.Class.Where(row => row.Semester == semester && row.Building == building && row.Room == room).ToList();
            }

            return classes;
        }

        public IActionResult GetMenu(string semester)
        {
            return ViewComponent("Menu", new { semester = semester });
        }
    }
}
