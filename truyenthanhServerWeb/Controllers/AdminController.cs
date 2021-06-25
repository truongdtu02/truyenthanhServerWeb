using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using truyenthanhServerWeb.Models;
using truyenthanhServerWeb.ServerMp3;
using truyenthanhServerWeb.Services;

namespace truyenthanhServerWeb.Controllers
{
    //[Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        //private readonly User _user;
        //private readonly IHttpContextAccessor _httpContextAccessor;
        //public AdminController(IHttpContextAccessor httpContextAccessor)
        //{
        //    _httpContextAccessor = httpContextAccessor;
        //    string currentClient = _httpContextAccessor.HttpContext.User.Identity.Name;
        //    _user = UDPServer._userList.Find(u => u.account.Username == currentClient);
        //}

        [HttpGet]
        public IActionResult Accounts()
        {
            return View();
        }

        [HttpGet]
        //[Route("Admin/Devices/{indx:int=-1}")]
        //public IActionResult Devices(int indx) //indx of user in userList
        public IActionResult Devices()
        {
            return View();
            //if((indx >= 0) && (indx < UDPServer._userList.Count()))
            //{
            //    ViewData["user"] = UDPServer._userList[indx];
            //    return View();
            //}
            //else
            //{
            //    return RedirectToAction("Accounts");
            //}
        }
    }
}

