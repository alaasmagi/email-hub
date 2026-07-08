using Contracts.DataAccess;
using Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Web.MVC.Controllers;

[Authorize]
public class TemplatesController : Controller
{
    private readonly ISenderIdentityRepository _senderIdentityRepository;
    private readonly ITemplateRepository _templateRepository;

    public TemplatesController(
        ISenderIdentityRepository senderIdentityRepository,
        ITemplateRepository templateRepository)
    {
        _senderIdentityRepository = senderIdentityRepository;
        _templateRepository = templateRepository;
    }

    public async Task<IActionResult> Index()
    {
        var templates = await _templateRepository.GetAllForAdminAsync();
        return View(templates);
    }

    public async Task<IActionResult> Create()
    {
        await LoadSenderIdentitiesAsync();
        return View(new Template { IsActive = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Template template)
    {
        if (!ModelState.IsValid)
        {
            await LoadSenderIdentitiesAsync();
            return View(template);
        }

        await _templateRepository.CreateAsync(template);

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var template = await _templateRepository.GetForAdminAsync(id);
        if (template is null)
        {
            return NotFound();
        }

        await LoadSenderIdentitiesAsync();
        return View(template);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, Template template)
    {
        if (id != template.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            await LoadSenderIdentitiesAsync();
            return View(template);
        }

        await _templateRepository.UpdateAsync(id, template, null, Guid.Empty);

        return RedirectToAction(nameof(Index));
    }

    private async Task LoadSenderIdentitiesAsync()
    {
        var senderIdentities = (await _senderIdentityRepository.GetAllForAdminAsync())
            .OrderBy(x => x.EmailType)
            .ThenBy(x => x.FromAddress)
            .Select(x => new SelectListItem
            {
                Value = x.Id.ToString(),
                Text = $"{x.EmailType} - {x.FromAddress}"
            })
            .ToList();

        ViewBag.SenderIdentities = senderIdentities;
    }
}
