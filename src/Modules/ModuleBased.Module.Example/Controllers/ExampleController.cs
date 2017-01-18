using Microsoft.AspNetCore.Mvc;

namespace ModuleBased.Module.Example.Controllers
{
    public class ExampleController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
