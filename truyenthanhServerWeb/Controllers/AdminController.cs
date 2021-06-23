using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using truyenthanhServerWeb.Models;
using truyenthanhServerWeb.Services;

namespace truyenthanhServerWeb.Controllers
{
    public class AdminController : Controller
    {
        [HttpGet]
        public IActionResult Accounts()
        {
            return View();
        }
    }
}

