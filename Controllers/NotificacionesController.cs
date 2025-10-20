using Audicob.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Audicob.Models;
using System.Security.Claims;

namespace Audicob.Controllers
{
    [Authorize(Roles = "Supervisor")]
    public class NotificacionesController : Controller
    {
        private readonly INotificacionService _notificacionService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<NotificacionesController> _logger;

        public NotificacionesController(
            INotificacionService notificacionService,
            UserManager<ApplicationUser> userManager,
            ILogger<NotificacionesController> logger)
        {
            _notificacionService = notificacionService;
            _userManager = userManager;
            _logger = logger;
        }

        // Vista principal de notificaciones
        public async Task<IActionResult> Index()
        {
            try
            {
                var usuarioId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(usuarioId))
                    return Unauthorized();

                var notificaciones = await _notificacionService.ObtenerNotificacionesUsuario(usuarioId);
                return View(notificaciones);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar notificaciones");
                TempData["Error"] = "Error al cargar las notificaciones";
                return RedirectToAction("Index", "Home");
            }
        }

        // Marcar como leída desde la vista
        [HttpPost]
        public async Task<IActionResult> MarcarLeida(int id)
        {
            try
            {
                await _notificacionService.MarcarComoLeida(id);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al marcar notificación");
                TempData["Error"] = "Error al procesar la notificación";
                return RedirectToAction(nameof(Index));
            }
        }

        // Marcar todas como leídas
        [HttpPost]
        public async Task<IActionResult> MarcarTodasLeidas()
        {
            try
            {
                var usuarioId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(usuarioId))
                    return Unauthorized();

                var notificaciones = await _notificacionService.ObtenerNotificacionesUsuario(usuarioId);
                var notificacionesNoLeidas = notificaciones.Where(n => !n.Leida).ToList();
                
                if (notificacionesNoLeidas.Any())
                {
                    foreach (var notif in notificacionesNoLeidas)
                    {
                        await _notificacionService.MarcarComoLeida(notif.Id);
                    }
                    TempData["Success"] = $"{notificacionesNoLeidas.Count} notificación(es) marcada(s) como leída(s)";
                }
                else
                {
                    TempData["Info"] = "No hay notificaciones pendientes por marcar";
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al marcar todas las notificaciones");
                TempData["Error"] = "Error al procesar las notificaciones";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}