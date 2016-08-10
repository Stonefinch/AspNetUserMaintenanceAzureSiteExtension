using System.Web.Mvc;

namespace AspNetUserMaintenanceAzureSiteExtension.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult ListUsers()
        {
            return View();
        }
    }
}