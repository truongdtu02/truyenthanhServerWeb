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
    [Authorize(Roles = "User")]
    public class UserController : Controller
    {
        private readonly User _user;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public UserController(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;

            _user = UDPServer._userList.FirstOrDefault
                (u => u.account.Username == _httpContextAccessor.HttpContext.User.Identity.Name);
        }
        public IActionResult Index()
        {
            return View(_user.account);
            //return View();
        }
    }
}
