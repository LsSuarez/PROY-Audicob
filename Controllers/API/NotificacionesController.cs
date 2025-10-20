using System.Security.Claims;
using Audicob.Data;
using Audicob.Models;
using Audicob.Models.DTOs;
using Audicob.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Audicob.Controllers.API
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class NotificacionesController : ControllerBase
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

        [HttpGet("mis-notificaciones")]
        public async Task<IActionResult> ObtenerMisNotificaciones()
        {
            try
            {
                var usuarioId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(usuarioId))
                {
                    _logger.LogWarning("Usuario no autenticado intentó acceder a notificaciones");
                    return Unauthorized(new { error = "Usuario no autenticado" });
                }

                var notificaciones = await _notificacionService.ObtenerNotificacionesUsuario(usuarioId);
                
                // Convertir a DTO con fechas en hora de Perú
                var notificacionesDto = notificaciones
                    .Select(n => NotificacionDto.FromNotificacion(n))
                    .ToList();
                
                _logger.LogInformation($"Usuario {usuarioId} obtuvo {notificacionesDto.Count} notificaciones");
                
                return Ok(notificacionesDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener notificaciones");
                return StatusCode(500, new { error = "Error al obtener notificaciones", detalle = ex.Message });
            }
        }

        [HttpGet("no-leidas-count")]
        public async Task<IActionResult> ObtenerNotificacionesNoLeidasCount()
        {
            try
            {
                var usuarioId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(usuarioId))
                    return Unauthorized(new { error = "Usuario no autenticado" });

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
                var usuarioId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(usuarioId))
                    return Unauthorized(new { error = "Usuario no autenticado" });

                await _notificacionService.MarcarComoLeida(id);
                
                _logger.LogInformation($"Notificación {id} marcada como leída por usuario {usuarioId}");
                
                return Ok(new { mensaje = "Notificación marcada como leída", success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al marcar notificación {id}");
                return StatusCode(500, new { error = "Error al procesar la solicitud", detalle = ex.Message });
            }
        }
    }
}