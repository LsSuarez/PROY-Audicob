using Audicob.Data;
using Audicob.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Audicob.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class DeudorController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<DeudorController> _logger;

        public DeudorController(ApplicationDbContext db, ILogger<DeudorController> logger)
        {
            _db = db;
            _logger = logger;
        }

        // Helpers
        private static string NombreClienteSafe(Models.Cliente? c)
        {
            if (c == null) return "—";

            // Usa lo que realmente tengas en tu entidad Cliente:
            // Ajusta estos nombres si tus propiedades reales son otras.
            // Orden de preferencia: Nombre -> RazonSocial -> Email -> Id
            var nombre =
                (c.GetType().GetProperty("Nombre")?.GetValue(c) as string) ??
                (c.GetType().GetProperty("RazonSocial")?.GetValue(c) as string) ??
                (c.GetType().GetProperty("Email")?.GetValue(c) as string);

            return string.IsNullOrWhiteSpace(nombre) ? $"Cliente #{c.Id}" : nombre!;
        }

        private static NivelCriticidad CalcularCrit(int dias, decimal monto)
        {
            var crit = NivelCriticidad.Bajo;
            if (dias > 15 || monto >= 1000) crit = NivelCriticidad.Medio;
            if (dias > 30 || monto >= 2000) crit = NivelCriticidad.Alto;
            if (dias > 45 || (monto >= 2000 && dias >= 30)) crit = NivelCriticidad.Critico;
            return crit;
        }

        // GET: /Deudor?ordenarPor=Criticidad&buscar=...
        [HttpGet]
        public async Task<IActionResult> Index(string? ordenarPor = "Criticidad", string? buscar = null, CancellationToken ct = default)
        {
            try
            {
                var hoy = DateTime.UtcNow.Date;

                // Traemos solo lo necesario y calculamos el resto en memoria (sin DateDiffDay)
                var baseList = await _db.Deudas
                    .AsNoTracking()
                    .Include(d => d.Cliente)
                    .Select(d => new
                    {
                        d.Id,
                        d.ClienteId,
                        d.Cliente,
                        d.Monto,
                        d.FechaVencimiento
                    })
                    .ToListAsync(ct);

                var list = baseList.Select(x =>
                {
                    var dias = (x.FechaVencimiento < hoy) ? (hoy - x.FechaVencimiento.Date).Days : 0;
                    return new DeudorVM
                    {
                        DeudaId = x.Id,
                        ClienteId = x.ClienteId,
                        Cliente = NombreClienteSafe(x.Cliente),
                        Monto = x.Monto,
                        AntiguedadDias = dias,
                        Criticidad = CalcularCrit(dias, x.Monto)
                    };
                });

                // Filtro de búsqueda
                if (!string.IsNullOrWhiteSpace(buscar))
                    list = list.Where(x => x.Cliente.Contains(buscar, StringComparison.OrdinalIgnoreCase));

                // Ordenar
                list = (ordenarPor?.ToLowerInvariant()) switch
                {
                    "monto"       => list.OrderByDescending(x => x.Monto),
                    "antigüedad"  => list.OrderByDescending(x => x.AntiguedadDias),
                    "criticidad"  => list.OrderByDescending(x => x.Criticidad),
                    _             => list.OrderBy(x => x.Cliente)
                };

                ViewBag.OrdenarPor = ordenarPor;
                ViewBag.Buscar = buscar;

                return View(list.ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en Deudor/Index");
                TempData["Error"] = "No se pudo cargar la lista de deudores.";
                return View(new List<DeudorVM>());
            }
        }

        // POST: /Deudor/Priorizar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Priorizar(string? ordenarPor, string? buscar, CancellationToken ct = default)
        {
            var result = await Index("Criticidad", buscar, ct) as ViewResult;
            if (result != null && result.Model is List<DeudorVM> lista)
            {
                var priorizada = lista
                    .OrderByDescending(x => x.Criticidad)
                    .ThenByDescending(x => x.AntiguedadDias)
                    .ThenByDescending(x => x.Monto)
                    .ToList();

                ViewBag.OrdenarPor = ordenarPor ?? "Criticidad";
                ViewBag.Buscar = buscar;
                return View("Index", priorizada);
            }

            // CS0019: No uses '??' entre tipos distintos
            return RedirectToAction(nameof(Index));
        }

        // GET: /Deudor/Detalle/5
        [HttpGet]
        public async Task<IActionResult> Detalle(int id, CancellationToken ct = default)
        {
            try
            {
                var hoy = DateTime.UtcNow.Date;

                var d = await _db.Deudas
                    .AsNoTracking()
                    .Include(x => x.Cliente)
                    .Where(x => x.Id == id)
                    .Select(x => new
                    {
                        x.Id,
                        x.ClienteId,
                        x.Cliente,
                        x.Monto,
                        x.FechaVencimiento
                    })
                    .FirstOrDefaultAsync(ct);

                if (d == null) return NotFound();

                var dias = (d.FechaVencimiento < hoy) ? (hoy - d.FechaVencimiento.Date).Days : 0;

                var vm = new DeudorVM
                {
                    DeudaId = d.Id,
                    ClienteId = d.ClienteId,
                    Cliente = NombreClienteSafe(d.Cliente),
                    Monto = d.Monto,
                    AntiguedadDias = dias,
                    Criticidad = CalcularCrit(dias, d.Monto)
                };

                ViewBag.FechaVencimiento = d.FechaVencimiento;
                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en Deudor/Detalle({Id})", id);
                return NotFound();
            }
        }
    }
}
