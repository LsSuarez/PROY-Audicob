using Audicob.Data;
using Audicob.Models;
using Audicob.Models.ViewModels.Cliente;
using Audicob.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Audicob.Controllers
{
    public class AsigSupervisorController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificacionService _notificacionService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<AsigSupervisorController> _logger;

        public AsigSupervisorController(ApplicationDbContext context,
            INotificacionService notificacionService,
            UserManager<ApplicationUser> userManager,
            ILogger<AsigSupervisorController> logger)
        {
            _context = context;
            _notificacionService = notificacionService;
            _userManager = userManager;
            _logger = logger;
        }

        public IActionResult Index(string? filtro, decimal? deudaMin, decimal? deudaMax, string? clasificacion)
        {
            // Buscar clientes sin asignar (donde AsignacionAsesorId es NULL)
            var query = _context.Clientes
                .Include(c => c.AsignacionAsesor)
                .Include(c => c.Deuda)
                .Where(c => c.AsignacionAsesorId == null);

            // Filtro por nombre o documento
            if (!string.IsNullOrEmpty(filtro))
            {
                query = query.Where(c =>
                    c.Nombre.Contains(filtro) ||
                    c.Documento.Contains(filtro));
            }

            // Filtro por rango de deuda
            if (deudaMin.HasValue)
            {
                query = query.Where(c => c.DeudaTotal >= deudaMin.Value);
            }

            if (deudaMax.HasValue)
            {
                query = query.Where(c => c.DeudaTotal <= deudaMax.Value);
            }

            // Filtro por clasificación
            if (!string.IsNullOrEmpty(clasificacion) && clasificacion != "")
            {
                query = query.Where(c => c.Deuda != null && c.Deuda.Clasificacion == clasificacion);
            }

            var clientes = query.ToList();

            var model = new ClienteDashboardViewModel
            {
                Filtro = filtro,
                DeudaMin = deudaMin,
                DeudaMax = deudaMax,
                Clasificacion = clasificacion,
                ListCliente = clientes
            };

            return View(model);
        }

        //Asignación de Cliente
        public IActionResult Asignar(int id)
        {
            var cliente = _context.Clientes.FirstOrDefault(c => c.Id == id);
            if (cliente == null) return NotFound();

            var asesores = _context.AsignacionesAsesores.ToList();

            ViewBag.ClienteId = id;
            ViewBag.ClienteNombre = cliente.Nombre;

            return PartialView("_AsignarAsesorPartial", asesores);
        }

        public IActionResult ObtenerAsesores()
        {
            try
            {
                var asesores = _context.AsignacionesAsesores
                    .Select(a => new { id = a.Id, nombre = a.AsesorNombre })
                    .OrderBy(a => a.nombre)
                    .ToList();

                return Json(asesores);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        public IActionResult ObtenerAsignaciones()
        {
            var lista = _context.AsignacionesAsesores
                .Include(a => a.Clientes)
                .ToList();

            return PartialView("_TablaAsignacionesPartial", lista);
        }

        public async Task<IActionResult> GuardarAsignacion(int clienteId, int asesorId)
        {
            try
            {
                var cliente = _context.Clientes.FirstOrDefault(c => c.Id == clienteId);
                var asignacion = _context.AsignacionesAsesores
                    .Include(a => a.Clientes)
                    .FirstOrDefault(a => a.Id == asesorId);

                if (cliente == null || asignacion == null)
                    return NotFound("Cliente o Asesor no encontrado");

                // Asignar el cliente al asesor
                cliente.AsignacionAsesor = asignacion;
                _context.SaveChanges();

                // OBTENER EL SUPERVISOR ACTUAL (el que está realizando la asignación)
                var usuarioActual = await _userManager.GetUserAsync(User);
                
                if (usuarioActual != null)
                {
                    // CREAR NOTIFICACIÓN PARA EL SUPERVISOR
                    var notificacion = new Notificacion
                    {
                        SupervisorId = usuarioActual.Id, // ID del Supervisor actual
                        Titulo = "📋 Nueva Asignación Realizada",
                        Descripcion = $"Se ha asignado el cliente {cliente.Nombre} al asesor {asignacion.AsesorNombre}. Deuda total: ${cliente.DeudaTotal:N2}",
                        AsignacionAsesorId = asesorId,
                        ClienteId = clienteId,
                        TipoNotificacion = "NuevaAsignacion",
                        IconoTipo = "📋",
                        Leida = false
                    };

                    await _notificacionService.CrearNotificacion(notificacion);
                    _logger.LogInformation($"Notificación creada para supervisor {usuarioActual.Id} - Cliente {cliente.Nombre} asignado a {asignacion.AsesorNombre}");
                }

                var lista = _context.AsignacionesAsesores
                    .Include(a => a.Clientes)
                    .ToList();

                return PartialView("_TablaAsignacionesPartial", lista);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al guardar asignación");
                return BadRequest($"Error al guardar: {ex.Message}");
            }
        }
    }
}