using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Audicob.Controllers
{
    using Audicob.Data;
    using Audicob.Models;
    using Audicob.Services;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using System.Security.Claims;

    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class NotificacionesController : ControllerBase
    {
        private readonly INotificacionService _notificacionService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<NotificacionesController> _logger;

        public NotificacionesController(INotificacionService notificacionService,
            UserManager<ApplicationUser> userManager,
            ILogger<NotificacionesController> logger)
        {
            _notificacionService = notificacionService;
            _userManager = userManager;
            _logger = logger;
        }

        [HttpGet("mis-notificaciones")]
        public async Task<IActionResult> ObtenerMisNotificaciones()
        {
            try
            {
                var usuarioId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(usuarioId))
                    return Unauthorized();

                var notificaciones = await _notificacionService.ObtenerNotificacionesUsuario(usuarioId);
                return Ok(notificaciones);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener notificaciones");
                return StatusCode(500, new { error = "Error al obtener notificaciones" });
            }
        }

        [HttpGet("no-leidas-count")]
        public async Task<IActionResult> ObtenerNotificacionesNoLeidasCount()
        {
            try
            {
                var usuarioId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(usuarioId))
                    return Unauthorized();

                var count = await _notificacionService.ObtenerNotificacionesNoLeidasCount(usuarioId);
                return Ok(new { noLeidas = count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener count de notificaciones");
                return StatusCode(500, new { error = "Error al obtener notificaciones" });
            }
        }

        [HttpPost("marcar-leida/{id}")]
        public async Task<IActionResult> MarcarComoLeida(int id)
        {
            try
            {
                await _notificacionService.MarcarComoLeida(id);
                return Ok(new { mensaje = "Notificación marcada como leída" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al marcar notificación");
                return StatusCode(500, new { error = "Error al procesar la solicitud" });
            }
        }
    }
}