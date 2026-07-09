using Contracts.DataAccess;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Web.MVC.Controllers;

[Authorize]
public class EmailController : Controller
{
    private readonly IEmailRepository _emailRepository;

    public EmailController(IEmailRepository emailRepository)
    {
        _emailRepository = emailRepository;
    }

    public async Task<IActionResult> Index()
    {
        var emails = await _emailRepository.GetAllForAdminAsync();
        return View(emails);
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var email = await _emailRepository.GetForAdminAsync(id);

        if (email is null)
        {
            return NotFound();
        }

        return View(email);
    }
}
