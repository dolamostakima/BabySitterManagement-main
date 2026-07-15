using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SmartBabySitter.Controllers;

public class WebController : Controller
{
    public IActionResult Index() => View();
    public IActionResult Login() => View();
    public IActionResult Register() => View();

    public IActionResult ParentDashboard() => View();
    public IActionResult SitterDashboard() => View();
    public IActionResult RegisterParent() => View();
    public IActionResult RegisterBabysitterType() => View();
    public IActionResult RegisterIndividual() => View();
    public IActionResult RegisterOrganization() => View();
    public IActionResult Sitters() => View();
    public IActionResult SitterDetails(int id) => View(model: id);

    public IActionResult ParentBookings() => View();

    public IActionResult ParentPayment(int? bookingId)
    {
        ViewBag.BookingId = bookingId;
        return View();
    }

    public IActionResult SitterBookings() => View();

    public IActionResult ReviewCreate(int id) => View(id);

    public IActionResult AdminDashboard() => View();
    public IActionResult AdminSitters() => View();
    public IActionResult AdminBookings() => View();
    public IActionResult AdminBookingDetails(int id) => View(model: id);
    public IActionResult AdminUsers() => View();
    public IActionResult AdminPayments() => View();
    public IActionResult AdminReviews() => View();

    public IActionResult Favorites() => View();
    public IActionResult Notifications() => View();

    public IActionResult SitterProfile() => View();
    public IActionResult SitterAvailability() => View();
    public IActionResult SitterReviews() => View();

    public IActionResult MyReviews() => View();

    public IActionResult ParentPaymentHistory()
    {
        return View();
    }

    public IActionResult SitterPaymentHistory()
    {
        return View();
    }

    public IActionResult SitterAttendance()
    {
        return View();
    }

    public IActionResult ParentAttendance()
    {
        return View();
    }

}