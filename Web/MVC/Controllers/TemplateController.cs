using Base.Contracts.DataAccess;
using Contracts.DataAccess;
using Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Web.MVC.Controllers;

[Authorize]
public class TemplateController : Controller
{
    private readonly ISenderIdentityRepository _senderIdentityRepository;
    private readonly ITemplateRepository _templateRepository;
    private readonly IBaseUow _uow;

    public TemplateController(
        ISenderIdentityRepository senderIdentityRepository,
        ITemplateRepository templateRepository,
        IBaseUow uow)
    {
        _senderIdentityRepository = senderIdentityRepository;
        _templateRepository = templateRepository;
        _uow = uow;
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

        var response = await _templateRepository.CreateAsync(template);
        if (!response.Successful)
        {
            ModelState.AddModelError(string.Empty, response.Error!.Message);
            await LoadSenderIdentitiesAsync();
            return View(template);
        }

        await _uow.SaveChangesAsync();

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

        var response = await _templateRepository.UpdateAsync(id, template, null, Guid.Empty);
        if (!response.Successful)
        {
            ModelState.AddModelError(string.Empty, response.Error!.Message);
            await LoadSenderIdentitiesAsync();
            return View(template);
        }

        await _uow.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var response = await _templateRepository.RemoveAsync(id, null, Guid.Empty);
            if (response.Successful)
            {
                await _uow.SaveChangesAsync();
            }

            TempData[response.Successful ? "SuccessMessage" : "ErrorMessage"] = response.Successful
                ? "Template deleted."
                : response.Error!.Message;
        }
        catch (DbUpdateException)
        {
            TempData["ErrorMessage"] = "Template could not be deleted.";
        }

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
