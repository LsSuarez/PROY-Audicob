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
        private readonly IPdfService _pdfService;

        public NotificacionesController(
            INotificacionService notificacionService,
            UserManager<ApplicationUser> userManager,
            ILogger<NotificacionesController> logger,
            IPdfService pdfService)
        {
            _notificacionService = notificacionService;
            _userManager = userManager;
            _logger = logger;
            _pdfService = pdfService;
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

        // Exportar notificaciones a PDF
        [HttpGet]
        public async Task<IActionResult> ExportarPdf()
        {
            try
            {
                var usuarioId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(usuarioId))
                    return Unauthorized();

                var usuario = await _userManager.FindByIdAsync(usuarioId);
                var notificaciones = await _notificacionService.ObtenerNotificacionesUsuario(usuarioId);

                var pdfBytes = _pdfService.GenerarPdfNotificaciones(notificaciones, usuario.UserName);

                return File(pdfBytes, "application/pdf", "Notificaciones.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al exportar PDF");
                TempData["Error"] = "Error al generar el PDF";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}