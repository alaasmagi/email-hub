using Base.Contracts.DataAccess;
using Contracts.DataAccess;
using Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Web.MVC.Controllers;

[Authorize]
public class ClientsController : Controller
{
    private readonly IClientRepository _clientRepository;
    private readonly IBaseUow _uow;

    public ClientsController(IClientRepository clientRepository, IBaseUow uow)
    {
        _clientRepository = clientRepository;
        _uow = uow;
    }

    public async Task<IActionResult> Index()
    {
        var clients = await _clientRepository.GetAllForAdminAsync();
        return View(clients);
    }

    public IActionResult Create()
    {
        return View(new Client());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Client client)
    {
        if (!ModelState.IsValid)
        {
            return View(client);
        }

        var response = await _clientRepository.CreateAsync(client);
        if (!response.Successful)
        {
            ModelState.AddModelError(string.Empty, response.Error!.Message);
            return View(client);
        }

        await _uow.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var client = await _clientRepository.GetForAdminAsync(id);
        if (client is null)
        {
            return NotFound();
        }

        return View(client);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, Client client)
    {
        if (id != client.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(client);
        }

        var response = await _clientRepository.UpdateAsync(id, client, null, Guid.Empty);
        if (!response.Successful)
        {
            ModelState.AddModelError(string.Empty, response.Error!.Message);
            return View(client);
        }

        await _uow.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }
}
