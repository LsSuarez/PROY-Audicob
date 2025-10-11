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
            try
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ConsultarDeuda cliente {ClienteId}", clienteId);
                TempData["Error"] = "Error al consultar la deuda.";
                return RedirectToAction(nameof(Dashboard));
            }
        }

        // Actualizar penalidad (AC2) — acepta returnUrl para volver a la lista con el mismo filtro
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActualizarPenalidad(int clienteId, string? returnUrl)
        {
            try
            {
                var cliente = await _db.Clientes
                    .Include(c => c.Deuda)
                    .FirstOrDefaultAsync(c => c.Id == clienteId);

                if (cliente == null || cliente.Deuda == null)
                {
                    TempData["Error"] = "Cliente o deuda no encontrada.";
                    return RedirectToAction(nameof(Dashboard));
                }

                var deuda = cliente.Deuda;
                var dias = CalcularDiasAtraso(deuda.FechaVencimiento);
                var penalidad = Math.Round(CalcularPenalidad(deuda.Monto, dias), 2);

                deuda.PenalidadCalculada = penalidad;
                deuda.Intereses = penalidad;
                deuda.TotalAPagar = deuda.Monto + penalidad;

                await _db.SaveChangesAsync();

                TempData["Success"] = $"Penalidad actualizada: S/ {penalidad:N2}";

                if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return LocalRedirect(returnUrl);

                return RedirectToAction(nameof(ConsultarDeuda), new { clienteId });
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Concurrencia al actualizar penalidad {ClienteId}", clienteId);
                TempData["Error"] = "Otro proceso actualizó esta deuda. Intenta de nuevo.";
                return RedirectToAction(nameof(ConsultarDeuda), new { clienteId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar penalidad {ClienteId}", clienteId);
                TempData["Error"] = "Error al actualizar la penalidad.";
                return RedirectToAction(nameof(ConsultarDeuda), new { clienteId });
            }
        }

        // Detalle de cálculo paso a paso
        [HttpGet]
        public async Task<IActionResult> VerDetallesCalculados(int clienteId)
        {
            try
            {
                var cliente = await ObtenerClienteConDeudaAsync(clienteId);
                if (cliente == null || cliente.Deuda == null)
                {
                    TempData["Error"] = "Cliente o deuda no encontrada.";
                    return RedirectToAction(nameof(Dashboard));
                }

                var deuda = cliente.Deuda;
                var dias = CalcularDiasAtraso(deuda.FechaVencimiento);
                var tasaDiaria = TasaPenalidadMensual / DiasPorMes;
                var penalidad = Math.Round(deuda.Monto * tasaDiaria * dias, 2);

                var model = new CalculoPenalidadDetalleViewModel
                {
                    ClienteNombre = cliente.Nombre,
                    MontoOriginal = deuda.Monto,
                    FechaVencimiento = deuda.FechaVencimiento,
                    DiasDeAtraso = dias,
                    TasaPenalidadMensual = TasaPenalidadMensual,
                    TasaPenalidadDiaria = tasaDiaria,
                    PenalidadCalculada = penalidad,
                    TotalAPagar = deuda.Monto + penalidad,
                    FormulaTexto = "Penalidad = Monto × TasaDiaria × DíasAtraso",
                    Paso1 = $"Tasa Mensual = {TasaPenalidadMensual:P2}",
                    Paso2 = $"Tasa Diaria = {TasaPenalidadMensual:P4} ÷ 30 = {tasaDiaria:P4}",
                    Paso3 = $"Penalidad = S/ {deuda.Monto:N2} × {tasaDiaria:P4} × {dias}",
                    Paso4 = $"Penalidad = S/ {penalidad:N2}"
                };

                ViewBag.ClienteId = clienteId;
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error VerDetallesCalculados {ClienteId}", clienteId);
                TempData["Error"] = "Error al calcular los detalles.";
                return RedirectToAction(nameof(Dashboard));
            }
        }

        // Generar comprobante PDF
        [HttpGet]
        public async Task<IActionResult> GenerarComprobante(int clienteId)
        {
            try
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
            catch (Exception ex)
            {
                // IMPORTANTE: deja el detalle de error real en TempData para depurar rápido
                _logger.LogError(ex, "Error GenerarComprobante {ClienteId}", clienteId);
                TempData["Error"] = $"Error al generar el comprobante: {ex.GetType().Name} - {ex.Message}";
                return RedirectToAction(nameof(ConsultarDeuda), new { clienteId });
            }
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
                // Log del convertidor y re-lanzamos para que el catch del Action maneje TempData + redirect
                _logger.LogError(ex, "DinkToPdf error: {Message}", ex.Message);
                throw;
            }
        }
    }
}
