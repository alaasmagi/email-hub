using Base.Contracts.DataAccess;
using Contracts.DataAccess;
using Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Web.MVC.Controllers;

[Authorize]
public class SenderIdentityController : Controller
{
    private readonly IClientRepository _clientRepository;
    private readonly ISenderIdentityRepository _senderIdentityRepository;
    private readonly IBaseUow _uow;

    public SenderIdentityController(
        IClientRepository clientRepository,
        ISenderIdentityRepository senderIdentityRepository,
        IBaseUow uow)
    {
        _clientRepository = clientRepository;
        _senderIdentityRepository = senderIdentityRepository;
        _uow = uow;
    }

    public async Task<IActionResult> Index()
    {
        var senderIdentities = await _senderIdentityRepository.GetAllForAdminAsync();
        return View(senderIdentities);
    }

    public async Task<IActionResult> Create()
    {
        await LoadClientsAsync();
        return View(new SenderIdentity());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SenderIdentity senderIdentity)
    {
        if (!ModelState.IsValid)
        {
            await LoadClientsAsync();
            return View(senderIdentity);
        }

        var response = await _senderIdentityRepository.CreateAsync(senderIdentity);
        if (!response.Successful)
        {
            ModelState.AddModelError(string.Empty, response.Error!.Message);
            await LoadClientsAsync();
            return View(senderIdentity);
        }

        await _uow.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var senderIdentity = await _senderIdentityRepository.GetForAdminAsync(id);
        if (senderIdentity is null)
        {
            return NotFound();
        }

        await LoadClientsAsync();
        return View(senderIdentity);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, SenderIdentity senderIdentity)
    {
        if (id != senderIdentity.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            await LoadClientsAsync();
            return View(senderIdentity);
        }

        var response = await _senderIdentityRepository.UpdateAsync(id, senderIdentity, null, Guid.Empty);
        if (!response.Successful)
        {
            ModelState.AddModelError(string.Empty, response.Error!.Message);
            await LoadClientsAsync();
            return View(senderIdentity);
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
            var response = await _senderIdentityRepository.RemoveAsync(id, null, Guid.Empty);
            if (response.Successful)
            {
                await _uow.SaveChangesAsync();
            }

            TempData[response.Successful ? "SuccessMessage" : "ErrorMessage"] = response.Successful
                ? "Sender identity deleted."
                : response.Error!.Message;
        }
        catch (DbUpdateException)
        {
            TempData["ErrorMessage"] = "Sender identity cannot be deleted while templates reference it.";
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task LoadClientsAsync()
    {
        var clients = (await _clientRepository.GetAllForAdminAsync())
            .OrderBy(x => x.ServiceName)
            .Select(x => new SelectListItem
            {
                Value = x.Id.ToString(),
                Text = $"{x.ServiceName} - {x.DisplayName}"
            })
            .ToList();

        ViewBag.Clients = clients;
    }
}
