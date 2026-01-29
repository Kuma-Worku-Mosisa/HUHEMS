using Microsoft.AspNetCore.Mvc;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        // If the user is already logged in, redirect them to their dashboard
        if (User.Identity.IsAuthenticated)
        {
            if (User.IsInRole("Student"))
                return RedirectToAction("Index", "Student");
            else
                return RedirectToAction("Index", "Exams");
        }
        return View();
    }
}