using gosarajevovol3.Data;
using gosarajevovol3.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using gosarajevovol3.Services.Abstract;

namespace gosarajevovol3.Controllers;

[Authorize]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class OperatorController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<User> _userManager;
    private readonly IEmailService _emailService;

    public OperatorController(ApplicationDbContext context, UserManager<User> userManager, IEmailService emailService)
    {
        _context = context;
        _userManager = userManager;
        _emailService = emailService;
    }
    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser is not Operator)
        {
            context.Result = new ForbidResult();
            return;
        }
        await next();
    }
    public async Task<IActionResult> Index()
    {
        var sviKorisnici = await _context.RegisteredUsers.ToListAsync();

        ViewBag.TotalSubscribers = sviKorisnici.Count;

        return View(sviKorisnici);
    }
    [HttpGet]
    public IActionResult CreateNewsletter()
    {
        return View();
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendNewsletter(string subject, string messageContent)
    {
        if (string.IsNullOrEmpty(subject) || string.IsNullOrEmpty(messageContent))
        {
            ModelState.AddModelError("", "Subject and Content cannot be empty.");
            return View("CreateNewsletter");
        }
        var sviEmailovi = await _context.RegisteredUsers
            .Select(u => u.Email)
            .Where(email => !string.IsNullOrEmpty(email)) 
            .ToListAsync();

        if (!sviEmailovi.Any())
        {
            TempData["SuccessMessage"] = "No registered users found.";
            return RedirectToAction("Index");
        }
        try
        {
            var response = await _emailService.SendToManyAsync(sviEmailovi!, subject, messageContent, messageContent);

            if (response.Success)
            {
                TempData["SuccessMessage"] = $"Newsletter successfully sent to all {sviEmailovi.Count} registered users!";
            }
            else
            {
                ModelState.AddModelError("", $"API Error (Status: {response.StatusCode}): {response.Body}");
                return View("CreateNewsletter");
            }
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", $"An error occurred while connecting: {ex.Message}");
            return View("CreateNewsletter");
        }
        
        return RedirectToAction("Index");
    }
}