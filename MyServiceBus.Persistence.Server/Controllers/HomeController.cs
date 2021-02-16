using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;

namespace MyServiceBus.Persistence.Server.Controllers
{
    [OpenApiIgnore]
    public class HomeController : Controller
    {
        [HttpGet("/")]
        public IActionResult Index()
        {
            return View();
        }
    }
}