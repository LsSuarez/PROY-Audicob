using Audicob.Data;
using Audicob.Models;
using Audicob.Models.ViewModels.Supervisor;
using Audicob.Models.ViewModels.Cobranza;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Audicob.Controllers
{
    [Authorize(Roles = "Supervisor")]
    public class SupervisorController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<SupervisorController> _logger;

        public SupervisorController(ApplicationDbContext db, UserManager<ApplicationUser> userManager, ILogger<SupervisorController> logger)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // =================== DASHBOARD ===================
        public async Task<IActionResult> Dashboard()
        {
            var vm = new SupervisorDashboardViewModel
            {
                TotalClientes = await _db.Clientes.CountAsync(),
                EvaluacionesPendientes = await _db.Evaluaciones.CountAsync(e => e.Estado == "Pendiente"),
                TotalDeuda = await _db.Clientes.SumAsync(c => c.DeudaTotal),
                TotalPagosUltimoMes = await _db.Pagos
                    .Where(p => p.Fecha >= DateTime.UtcNow.AddMonths(-1))
                    .SumAsync(p => p.Monto)
            };

            // Gráfico de pagos por mes (últimos 6 meses)
            var pagos = await _db.Pagos
                .Where(p => p.Fecha >= DateTime.UtcNow.AddMonths(-6))
                .GroupBy(p => new { p.Fecha.Year, p.Fecha.Month })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Total = g.Sum(x => x.Monto)
                })
                .OrderBy(x => x.Year).ThenBy(x => x.Month)
                .ToListAsync();

            vm.Meses = pagos.Select(g => $"{g.Month}/{g.Year}").ToList();
            vm.PagosPorMes = pagos.Select(g => g.Total).ToList();

            // Clientes con mayor deuda
            var deudas = await _db.Clientes
                .OrderByDescending(c => c.DeudaTotal)
                .Take(5)
                .Select(c => new { c.Nombre, c.DeudaTotal })
                .ToListAsync();

            vm.Clientes = deudas.Select(d => d.Nombre).ToList();
            vm.DeudasPorCliente = deudas.Select(d => d.DeudaTotal).ToList();

            // Pagos pendientes recientes
            vm.PagosPendientes = await _db.Pagos
                .Where(p => p.Estado == "Pendiente")
                .Include(p => p.Cliente)
                .OrderBy(p => p.Fecha)
                .Take(10)
                .ToListAsync();

            return View(vm);
        }

        // =================== HU7: VALIDAR PAGO ===================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ValidarPago(int pagoId)
        {
            var pago = await _db.Pagos.Include(p => p.Cliente).FirstOrDefaultAsync(p => p.Id == pagoId);

            if (pago == null)
            {
                TempData["Error"] = "Pago no encontrado.";
                return RedirectToAction("Dashboard");
            }

            if (pago.Estado != "Pendiente")
            {
                TempData["Error"] = "Este pago ya ha sido validado.";
                return RedirectToAction("Dashboard");
            }

            var user = await _userManager.GetUserAsync(User);
            pago.Validado = true;
            pago.Estado = "Cancelado";
            pago.Observacion = $"Validado por {user.FullName} el {DateTime.UtcNow:dd/MM/yyyy HH:mm:ss}";

            if (pago.Cliente != null)
            {
                pago.Cliente.DeudaTotal -= pago.Monto;
                if (pago.Cliente.DeudaTotal < 0) pago.Cliente.DeudaTotal = 0;
                pago.Cliente.FechaActualizacion = DateTime.UtcNow;
            }

            _db.Update(pago);
            await _db.SaveChangesAsync();

            TempData["Success"] = $"Pago de S/ {pago.Monto:N2} validado exitosamente por {user.FullName}. Estado de cuenta actualizado.";
            return RedirectToAction("Dashboard");
        }

        // =================== HU22: VALIDAR PAGOS (ESTADOS Y CONTROL) ===================
        [HttpGet]
        public async Task<IActionResult> ValidarPagos()
        {
            try
            {
                var pagos = await _db.Pagos
                    .Include(p => p.Cliente)
                    .OrderByDescending(p => p.Fecha)
                    .ToListAsync();

                var vm = pagos.Select(p => new PagoValidacionViewModel
                {
                    PagoId = p.Id,
                    ClienteNombre = p.Cliente.Nombre,
                    Fecha = p.Fecha,
                    Monto = p.Monto,
                    Estado = p.Estado,
                    Validado = p.Validado,
                    Observacion = p.Observacion
                }).ToList();

                return View("ValidarPagos", vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar los pagos para validación");
                TempData["Error"] = "Ocurrió un error al cargar los pagos.";
                return RedirectToAction("Dashboard");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarEstadoPago(int id, string nuevoEstado)
        {
            try
            {
                var pago = await _db.Pagos
                    .Include(p => p.Cliente)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (pago == null)
                {
                    TempData["Error"] = "No se encontró el pago.";
                    return RedirectToAction(nameof(ValidarPagos));
                }

                pago.Estado = nuevoEstado;
                pago.Validado = (nuevoEstado == "Cancelado");

                if (pago.Validado && pago.Cliente != null)
                {
                    var deuda = await _db.Deudas.FirstOrDefaultAsync(d => d.ClienteId == pago.ClienteId);
                    if (deuda != null)
                    {
                        deuda.TotalAPagar = 0;
                        deuda.PenalidadCalculada = 0;
                        deuda.Monto = 0;
                    }

                    pago.Observacion = $"Pago validado como '{nuevoEstado}' el {DateTime.Now:dd/MM/yyyy HH:mm}";
                }

                await _db.SaveChangesAsync();

                TempData["Success"] = $"El estado del pago se actualizó a '{nuevoEstado}'.";
                return RedirectToAction(nameof(ValidarPagos));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cambiar el estado del pago {Id}", id);
                TempData["Error"] = "Error al cambiar el estado del pago.";
                return RedirectToAction(nameof(ValidarPagos));
            }
        }

        // =================== ASIGNACIÓN DE LÍNEA DE CRÉDITO ===================
        public async Task<IActionResult> AsignarLineaCredito(int id)
        {
            var cliente = await TryGetClienteAsync(id);
            if (cliente == null)
            {
                TempData["Error"] = "Cliente no encontrado.";
                return RedirectToAction("Dashboard");
            }

            if (cliente.LineaCredito != null)
            {
                TempData["Error"] = "Este cliente ya tiene una línea de crédito asignada.";
                return RedirectToAction("Dashboard");
            }

            var vm = new AsignacionLineaCreditoViewModel
            {
                ClienteId = cliente.Id,
                NombreCliente = cliente.Nombre,
                DeudaTotal = cliente.DeudaTotal,
                IngresosMensuales = cliente.IngresosMensuales
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AsignarLineaCredito(AsignacionLineaCreditoViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var cliente = await TryGetClienteAsync(model.ClienteId);
            if (cliente == null)
            {
                TempData["Error"] = "Cliente no válido.";
                return RedirectToAction("Dashboard");
            }

            if (cliente.LineaCredito != null)
            {
                TempData["Error"] = "Este cliente ya tiene una línea de crédito asignada.";
                return RedirectToAction("Dashboard");
            }

            if (model.MontoAsignado < 180)
            {
                ModelState.AddModelError("MontoAsignado", "El valor ingresado debe ser mayor que 180 soles.");
                model.NombreCliente = cliente.Nombre;
                model.DeudaTotal = cliente.DeudaTotal;
                model.IngresosMensuales = cliente.IngresosMensuales;
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);

            var linea = new LineaCredito
            {
                ClienteId = cliente.Id,
                Monto = model.MontoAsignado,
                FechaAsignacion = DateTime.UtcNow,
                UsuarioAsignador = user.FullName
            };

            _db.LineasCredito.Add(linea);
            await _db.SaveChangesAsync();

            TempData["Success"] = $"Línea de crédito de S/ {model.MontoAsignado:N2} asignada exitosamente a {cliente.Nombre} por {user.FullName}.";
            return RedirectToAction("Dashboard");
        }

        // =================== AUXILIARES ===================
        private async Task<Cliente?> TryGetClienteAsync(int clienteId)
        {
            return await _db.Clientes.Include(c => c.LineaCredito).FirstOrDefaultAsync(c => c.Id == clienteId);
        }
    }
}
