using Audicob.Data;
using Audicob.Models;
using Audicob.Models.ViewModels.Asesor;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Audicob.Controllers
{
    [Authorize(Roles = "AsesorCobranza")]
    public class AsesorController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public AsesorController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        // ===============================
        // DASHBOARD (YA EXISTENTE)
        // ===============================
        public async Task<IActionResult> Dashboard()
        {
            var user = await _userManager.GetUserAsync(User);
            var asignaciones = await _db.AsignacionesAsesores
                .Include(a => a.Clientes)
                .ThenInclude(c => c.Deuda)
                .Where(a => a.AsesorUserId == user.Id)
                .ToListAsync();

            var clientes = asignaciones.SelectMany(a => a.Clientes).ToList();

            var vm = new AsesorDashboardViewModel
            {
                TotalClientesAsignados = clientes.Count,
                TotalDeudaCartera = clientes.Sum(c => c.Deuda?.TotalAPagar ?? 0),
                TotalPagosRecientes = await _db.Pagos
                    .Where(p => clientes.Select(c => c.Id).Contains(p.ClienteId) &&
                                p.Fecha >= DateTime.UtcNow.AddMonths(-1))
                    .SumAsync(p => p.Monto),
                Clientes = clientes.Select(c => c.Nombre).ToList(),
                DeudasPorCliente = clientes.Select(c => c.Deuda?.TotalAPagar ?? 0).ToList()
            };

            return View(vm);
        }

        // ==========================================================
        // NUEVA FUNCIÓN: REGISTRO E HISTORIAL CREDITICIO DEL CLIENTE
        // ==========================================================

        [HttpGet]
        public async Task<IActionResult> HistorialCredito()
        {
            var registros = await _db.HistorialCreditos
                .OrderByDescending(h => h.FechaOperacion)
                .ToListAsync();

            // El modelo que se envía a la vista será una lista de registros
            return View(registros);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HistorialCredito(HistorialCredito model)
        {
            if (ModelState.IsValid)
            {
                model.FechaOperacion = DateTime.SpecifyKind(model.FechaOperacion, DateTimeKind.Utc);
                // Guardar los datos en la BD
                _db.HistorialCreditos.Add(model);
                await _db.SaveChangesAsync();

                ViewBag.Mensaje = "✅ Datos guardados correctamente en el historial.";

                // Limpiar el modelo para que los campos del formulario se vacíen
                ModelState.Clear();
                model = new HistorialCredito();
            }

            // Mostrar nuevamente el historial
            var registros = await _db.HistorialCreditos
                .OrderByDescending(h => h.FechaOperacion)
                .ToListAsync();

            // Devuelve la vista con los registros actualizados
            return View(registros);
        }
        // ==========================================================
        // EDITAR REGISTRO EXISTENTE EN EL HISTORIAL
        // ==========================================================

        // GET: Muestra el formulario de edición
        [HttpGet]
        public async Task<IActionResult> EditarHistorial(int id)
        {
            var registro = await _db.HistorialCreditos.FindAsync(id);
            if (registro == null)
            {
                return NotFound();
            }
            return View(registro);
        }

        // POST: Guarda los cambios del registro editado
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarHistorial(HistorialCredito model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // ✅ Asegurar que la fecha sea UTC para evitar el error de PostgreSQL
            model.FechaOperacion = DateTime.SpecifyKind(model.FechaOperacion, DateTimeKind.Utc);

            _db.HistorialCreditos.Update(model);
            await _db.SaveChangesAsync();

            TempData["Mensaje"] = "✅ Registro actualizado correctamente.";
            return RedirectToAction(nameof(HistorialCredito));
        }

    }
}
