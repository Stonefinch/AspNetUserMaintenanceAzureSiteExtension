using AspNetUserMaintenanceAzureSiteExtension.Models;
using AspNetUserMaintenanceAzureSiteExtension.Services;
using System.Web.Mvc;
using System.Linq;

namespace AspNetUserMaintenanceAzureSiteExtension.Controllers
{
    public class HomeController : Controller
    {
        private IUserRepository UserRepository { get; set; }

        public HomeController()
        {
            this.UserRepository = new UserRepository(new AzureConfiguration());
        }

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult ListUsers(string u, string e, string r)
        {
            var sc = new AspNetUserSearchCriteria()
            {
                UserName = u,
                Email = e,
                Role = r
            };

            ViewBag.Sc = sc;
            
            ViewBag.AspNetUsers = this.UserRepository.ListAspNetUsers(sc).ToList();

            return View();
        }

        public ActionResult CreateUser()
        {
            return View();
        }

        [HttpPost]
        public ActionResult CreateUser(string u, string e, string p, string r)
        {
            var result = this.UserRepository.CreateUser(u, e, p, r);

            if (result != null && result.Count() > 0)
            {
                ViewBag.UserName = u;
                ViewBag.Email = e;
                ViewBag.Password = p;
                ViewBag.Roles = r;

                ViewBag.Errors = result;
                return View();
            }

            return RedirectToAction("ListUsers", new { u = u});
        }
    }
}