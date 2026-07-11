using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SmartBabySitter.Controllers
{
  
    public class ParentPaymentController : Controller
    {
        public IActionResult ParentPayment()
        {
            return View("~/Views/Web/ParentPayment.cshtml");
        }
    }
}