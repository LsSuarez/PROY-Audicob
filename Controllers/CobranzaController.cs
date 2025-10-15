using Audicob.Data;
using Audicob.Models;
using Audicob.Models.ViewModels.Cobranza;
using DinkToPdf;
using DinkToPdf.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using Audicob.Models.ViewModels.Asesor;
namespace Audicob.Controllers
{
    [Authorize(Roles = "AsesorCobranza")]
    public class CobranzaController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<CobranzaController> _logger;
        private readonly IConverter _pdf;

        private const decimal TasaPenalidadMensual = 0.015m; // 1.5%
        private const int DiasPorMes = 30;

        public CobranzaController(
            ApplicationDbContext db,
            ILogger<CobranzaController> logger,
            IConverter pdf)
        {
            _db = db;
            _logger = logger;
            _pdf = pdf;
        }

        // =================== Helpers ===================

        private static int CalcularDiasAtraso(DateTime fechaVencimiento)
            => Math.Max(0, (DateTime.Today - fechaVencimiento.Date).Days);

        private static decimal CalcularPenalidad(decimal monto, int diasAtraso)
        {
            if (diasAtraso <= 0) return 0m;
            var tasaDiaria = TasaPenalidadMensual / DiasPorMes;
            return monto * tasaDiaria * diasAtraso;
        }

        private async Task<Cliente?> ObtenerClienteConDeudaAsync(int clienteId) =>
            await _db.Clientes
                     .Include(c => c.Deuda)
                     .AsNoTracking()
                     .FirstOrDefaultAsync(c => c.Id == clienteId);

        private static string Html(string? s) => WebUtility.HtmlEncode(s ?? string.Empty);

        // =================== Acciones ===================

        // Acción GET para mostrar el formulario de pago
        [HttpGet]
        public async Task<IActionResult> RegistrarPago(int clienteId)
        {
            var cliente = await _db.Clientes
                                   .FirstOrDefaultAsync(c => c.Id == clienteId);

            if (cliente == null)
            {
                TempData["Error"] = "Cliente no encontrado.";
                return RedirectToAction(nameof(Dashboard));
            }

            var model = new RegistrarPagoViewModel
            {
                ClienteId = cliente.Id,
                ClienteNombre = cliente.Nombre,
                DeudaTotal = cliente.Deuda?.Monto ?? 0, // Si tiene deuda
                DeudaActual = cliente.Deuda?.Monto ?? 0 // Actual deuda
            };

            return View(model);
        }

        // Acción POST para registrar el pago
        [HttpPost]
        public async Task<IActionResult> RegistrarPago(RegistrarPagoViewModel model)
        {
            if (ModelState.IsValid)
            {
                var cliente = await _db.Clientes
                                       .FirstOrDefaultAsync(c => c.Id == model.ClienteId);

                if (cliente == null || cliente.Deuda == null)
                {
                    TempData["Error"] = "Cliente o deuda no encontrada.";
                    return RedirectToAction(nameof(Dashboard));
                }

                // Crear la transacción
                var transaccion = new Transaccion
                {
                    ClienteId = cliente.Id,
                    Monto = model.Monto,
                    Estado = model.Monto >= cliente.Deuda.Monto ? "PAGADA" : "PARCIALMENTE PAGADA",
                    Fecha = DateTime.UtcNow,
                    MetodoPago = model.MetodoPago, // Método de pago elegido
                    Observaciones = model.Observaciones // Observaciones del pago
                };

                // Actualizar deuda del cliente
                if (model.Monto >= cliente.Deuda.Monto)
                {
                    cliente.Deuda.Monto = 0; // Deuda completamente pagada
                }
                else
                {
                    cliente.Deuda.Monto -= model.Monto; // Pago parcial
                }

                // Guardar transacción y actualizar la deuda
                _db.Transacciones.Add(transaccion);
                _db.Clientes.Update(cliente);
                await _db.SaveChangesAsync();

                TempData["Success"] = "Pago registrado correctamente.";
                return RedirectToAction(nameof(Dashboard)); // Redirige después del registro del pago
            }

            // Si la validación falla, mostrar el formulario de nuevo con los errores
            return View(model);
        }

        // Panel de cobranzas por asignación del asesor
        [HttpGet]
        public async Task<IActionResult> Dashboard(string searchTerm = "")
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                var asignaciones = await _db.AsignacionesAsesores
                    .Include(a => a.Cliente)
                    .Where(a => a.AsesorUserId == userId)
                    .AsNoTracking()
                    .ToListAsync();

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    var t = searchTerm.Trim();
                    asignaciones = asignaciones.Where(a =>
                        (a.Cliente.Nombre ?? "").Contains(t, StringComparison.OrdinalIgnoreCase) ||
                        (a.Cliente.Documento ?? "").Contains(t, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                var vm = new CobranzaDashboardViewModel
                {
                    SearchTerm = searchTerm,
                    TotalClientesAsignados = asignaciones.Count,
                    TotalDeudaCartera = asignaciones.Sum(a => a.Cliente.DeudaTotal),
                    Clientes = asignaciones.Select(a => new ClienteDeudaViewModel
                    {
                        ClienteId = a.Cliente.Id,
                        ClienteNombre = a.Cliente.Nombre,
                        DeudaTotal = a.Cliente.DeudaTotal
                    }).ToList()
                };
                vm.VerificarResultadosBusqueda();

                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en Dashboard de Cobranza");
                TempData["Error"] = "Ocurrió un error al cargar el dashboard.";
                return RedirectToAction("Index", "Home");
            }
        }

        // Listado + buscador por DNI/nombre (Vista: Views/Cobranza/Clientes.cshtml)
        [HttpGet]
        public async Task<IActionResult> Clientes(string q = "")
        {
            try
            {
                var clientes = await _db.Clientes
                    .Include(c => c.Deuda)
                    .AsNoTracking()
                    .OrderBy(c => c.Nombre)
                    .ToListAsync();

                if (!string.IsNullOrWhiteSpace(q))
                {
                    var term = q.Trim();
                    clientes = clientes
                        .Where(c => (c.Documento ?? "").Contains(term, StringComparison.OrdinalIgnoreCase)
                                 || (c.Nombre ?? "").Contains(term, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                clientes = clientes.Take(10).ToList();

                var vm = new CobranzaDashboardViewModel
                {
                    SearchTerm = q,
                    TotalClientesAsignados = clientes.Count,
                    TotalDeudaCartera = clientes.Sum(c =>
                    {
                        if (c.Deuda != null)
                        {
                            var dias = CalcularDiasAtraso(c.Deuda.FechaVencimiento);
                            var pen = CalcularPenalidad(c.Deuda.Monto, dias);
                            return c.Deuda.Monto + pen;
                        }
                        return c.DeudaTotal;
                    }),
                    Clientes = clientes.Select(c =>
                    {
                        decimal total = c.DeudaTotal;
                        if (c.Deuda != null)
                        {
                            var dias = CalcularDiasAtraso(c.Deuda.FechaVencimiento);
                            var pen = CalcularPenalidad(c.Deuda.Monto, dias);
                            total = c.Deuda.Monto + pen;
                        }

                        return new ClienteDeudaViewModel
                        {
                            ClienteId = c.Id,
                            ClienteNombre = c.Nombre,
                            DeudaTotal = total
                        };
                    }).ToList()
                };

                vm.VerificarResultadosBusqueda();
                return View("Clientes", vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en Clientes (buscador)");
                TempData["Error"] = "Ocurrió un error al cargar el listado de clientes.";
                return RedirectToAction(nameof(Dashboard));
            }
        }

        // Detalle de deuda
        [HttpGet]
        public async Task<IActionResult> ConsultarDeuda(int clienteId)
        {
            var cliente = await ObtenerClienteConDeudaAsync(clienteId);
            if (cliente == null || cliente.Deuda == null)
            {
                TempData["Error"] = "Cliente o deuda no encontrada.";
                return RedirectToAction(nameof(Dashboard));
            }

            var deuda = cliente.Deuda;
            var dias = CalcularDiasAtraso(deuda.FechaVencimiento);
            var penalidad = Math.Round(CalcularPenalidad(deuda.Monto, dias), 2);

            var model = new DeudaDetalleViewModel
            {
                ClienteId = cliente.Id, // importante para botones
                Cliente = cliente.Nombre,
                MontoDeuda = deuda.Monto,
                DiasAtraso = dias,
                PenalidadCalculada = penalidad,
                TotalAPagar = deuda.Monto + penalidad,
                FechaVencimiento = deuda.FechaVencimiento,
                TasaPenalidad = TasaPenalidadMensual
            };

            return View("ConsultarDeuda", model);
        }

        // Método para generar comprobante PDF de la deuda
        [HttpGet]
        public async Task<IActionResult> GenerarComprobante(int clienteId)
        {
            var cliente = await ObtenerClienteConDeudaAsync(clienteId);
            if (cliente == null || cliente.Deuda == null)
            {
                TempData["Error"] = "Cliente o deuda no encontrada.";
                return RedirectToAction(nameof(Dashboard));
            }

            var deuda = cliente.Deuda;
            var dias = CalcularDiasAtraso(deuda.FechaVencimiento);
            var penalidad = Math.Round(CalcularPenalidad(deuda.Monto, dias), 2);

            var model = new ComprobanteDeudaPdfViewModel
            {
                Cliente = cliente.Nombre,
                MontoDeuda = deuda.Monto,
                DiasDeAtraso = dias,
                TasaPenalidad = TasaPenalidadMensual,
                PenalidadCalculada = penalidad,
                TotalAPagar = deuda.Monto + penalidad,
                FechaVencimiento = deuda.FechaVencimiento
            };

            var html = GenerateHtml(model);
            var bytes = GeneratePdf(html);

            var safeName = string.Join("_",
                (cliente.Nombre ?? "Cliente").Split(System.IO.Path.GetInvalidFileNameChars()));

            return File(bytes, "application/pdf",
                $"Comprobante_{safeName}_{DateTime.Now:yyyyMMddHHmm}.pdf");
        }

        // =================== PDF helpers ===================

        private string GenerateHtml(ComprobanteDeudaPdfViewModel m)
        {
            var cliente = Html(m.Cliente);

            return $@"
<html>
<head>
<meta charset='utf-8' />
<style>
body {{ font-family: Arial, sans-serif; margin: 32px; }}
h1 {{ color: #2c3e50; text-align: center; }}
.header {{ background:#3498db; color:#fff; padding:16px; text-align:center; border-radius:8px; }}
table {{ width:100%; border-collapse:collapse; margin-top:18px; }}
td {{ padding:10px 8px; border-bottom:1px solid #eee; }}
.label {{ font-weight:600; }}
.total {{ background:#e74c3c; color:#fff; padding:14px; text-align:center; font-size:18px; border-radius:8px; margin-top:18px; }}
.muted {{ color:#666; font-size:12px; text-align:right; }}
</style>
</head>
<body>
<div class='header'><h1>COMPROBANTE DE DEUDA</h1><div>Sistema de Cobranza AUDICOB</div></div>
<div class='muted'>Fecha de emisión: {DateTime.Now:dd/MM/yyyy HH:mm}</div>
<table>
<tr><td class='label'>Cliente:</td><td>{cliente}</td></tr>
<tr><td class='label'>Monto Original:</td><td>S/ {m.MontoDeuda:N2}</td></tr>
<tr><td class='label'>Fecha de Vencimiento:</td><td>{m.FechaVencimiento:dd/MM/yyyy}</td></tr>
<tr><td class='label'>Días de Atraso:</td><td>{m.DiasDeAtraso} días</td></tr>
<tr><td class='label'>Tasa Penalidad Mensual:</td><td>{m.TasaPenalidad:P2}</td></tr>
<tr><td class='label'>Penalidad Calculada:</td><td><b>S/ {m.PenalidadCalculada:N2}</b></td></tr>
</table>
<div class='total'><b>TOTAL A PAGAR: S/ {m.TotalAPagar:N2}</b></div>
</body>
</html>";
        }

        private byte[] GeneratePdf(string htmlContent)
        {
            try
            {
                var doc = new HtmlToPdfDocument
                {
                    GlobalSettings = new GlobalSettings
                    {
                        ColorMode = ColorMode.Color,
                        Orientation = Orientation.Portrait,
                        PaperSize = PaperKind.A4,
                        Margins = new MarginSettings { Top = 10, Bottom = 10, Left = 10, Right = 10 }
                    },
                    Objects =
                    {
                        new ObjectSettings
                        {
                            HtmlContent = htmlContent,
                            WebSettings = { DefaultEncoding = "utf-8", LoadImages = true }
                        }
                    }
                };

                return _pdf.Convert(doc);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DinkToPdf error: {Message}", ex.Message);
                throw;
            }
        }
    }
}
