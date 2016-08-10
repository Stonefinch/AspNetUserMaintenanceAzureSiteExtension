using AspNetUserMaintenanceAzureSiteExtension.Models;
using AspNetUserMaintenanceAzureSiteExtension.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

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

        public ActionResult CreateRole()
        {
            return View();
        }

        [HttpPost]
        public ActionResult CreateRole(string r)
        {
            var result = this.UserRepository.CreateRole(r);

            if (result != null && result.Count() > 0)
            {
                ViewBag.Role = r;

                ViewBag.Errors = result;

                return View();
            }

            return RedirectToAction("ListUsers");
        }

        public ActionResult BulkCreate()
        {
            return View();
        }

        [HttpPost]
        public ActionResult BulkCreate(string csv)
        {
            var result = new List<string>();

            // split csv into rows
            var rows = csv.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var row in rows)
            {
                try
                {
                    // split row into values
                    var vals = row.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToArray();

                    var userName = vals[0];
                    var email = vals[1];
                    var password = vals[2];
                    var roles = (vals.Count() == 4) ? vals[3] : "";

                    result.AddRange(this.UserRepository.CreateUser(userName, email, password, roles));
                }
                catch (Exception ex)
                {
                    result.Add($"Row [{row}] could not be processed.");
                    result.Add(ex.ToString());
                }
            }

            if (result != null && result.Count() > 0)
            {
                ViewBag.Csv = csv;

                ViewBag.Errors = result;
                return View();
            }

            return RedirectToAction("ListUsers");
        }
    }
}