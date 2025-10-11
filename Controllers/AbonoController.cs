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
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Audicob.Controllers
{
    [Authorize(Roles = "Cliente")]
    public class AbonoController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<AbonoController> _logger;
        private readonly IConverter _pdfConverter;

        public AbonoController(
            ApplicationDbContext db,
            ILogger<AbonoController> logger,
            IConverter pdfConverter)   // <-- inyectamos el convertidor como en Cobranza
        {
            _db = db;
            _logger = logger;
            _pdfConverter = pdfConverter;
        }

        // Helper: Id del usuario logueado (Identity)
        private string? CurrentUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier);

        // Helper: HTML encode
        private static string H(string? s) => WebUtility.HtmlEncode(s ?? string.Empty);

        // ================== HU6: Estado de Cuenta ==================
        [HttpGet]
        public async Task<IActionResult> EstadoCuenta()
        {
            try
            {
                var userId = CurrentUserId();

                var cliente = await _db.Clientes
                    .AsNoTracking()
                    .Include(c => c.Deuda)
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (cliente == null)
                {
                    TempData["Error"] = "No se encontró información del cliente.";
                    return RedirectToAction("Index", "Home");
                }

                if (cliente.Deuda == null)
                {
                    TempData["Warning"] = "No tiene deudas registradas.";
                    return RedirectToAction("Dashboard", "Cliente");
                }

                var historial = await _db.Transacciones
                    .AsNoTracking()
                    .Where(t => t.ClienteId == cliente.Id)
                    .OrderByDescending(t => t.Fecha)
                    .ToListAsync();

                var vm = new EstadoCuentaViewModel
                {
                    TotalDeuda = cliente.Deuda.TotalAPagar,
                    Capital = cliente.Deuda.Monto,
                    Intereses = cliente.Deuda.PenalidadCalculada,
                    HistorialTransacciones = historial
                };

                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cargando EstadoCuenta");
                TempData["Error"] = "Ocurrió un error al cargar el estado de cuenta.";
                return RedirectToAction("Index", "Home");
            }
        }

        // ================== HU6: Filtrar Historial (Partial) ==================
        [HttpGet]
        public async Task<IActionResult> FiltrarHistorial(
            string? searchTerm,
            decimal? montoMin,
            decimal? montoMax,
            DateTime? fechaDesde,
            DateTime? fechaHasta)
        {
            try
            {
                var userId = CurrentUserId();

                var cliente = await _db.Clientes
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (cliente == null)
                    return Json(new { error = "Cliente no encontrado" });

                var q = _db.Transacciones
                    .AsNoTracking()
                    .Where(t => t.ClienteId == cliente.Id);

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    var term = searchTerm.Trim();
                    q = q.Where(t =>
                        (t.Descripcion ?? "").ToLower().Contains(term.ToLower()) ||
                        (t.NumeroTransaccion ?? "").ToLower().Contains(term.ToLower()));
                }

                if (montoMin.HasValue)
                    q = q.Where(t => t.Monto >= montoMin.Value);

                if (montoMax.HasValue)
                    q = q.Where(t => t.Monto <= montoMax.Value);

                if (fechaDesde.HasValue)
                    q = q.Where(t => t.Fecha >= fechaDesde.Value.Date);

                if (fechaHasta.HasValue)
                {
                    // incluir todo el día fechaHasta (23:59:59)
                    var hasta = fechaHasta.Value.Date.AddDays(1).AddTicks(-1);
                    q = q.Where(t => t.Fecha <= hasta);
                }

                var historial = await q
                    .OrderByDescending(t => t.Fecha)
                    .ToListAsync();

                return PartialView("_HistorialTransacciones", historial);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error filtrando historial");
                return Json(new { error = "Ocurrió un error al filtrar el historial." });
            }
        }

        // ================== HU6: Ver Comprobante de Pago ==================
        [HttpGet]
        public async Task<IActionResult> VerComprobante(int transaccionId)
        {
            try
            {
                var userId = CurrentUserId();

                var transaccion = await _db.Transacciones
                    .AsNoTracking()
                    .Include(t => t.Cliente)
                    .FirstOrDefaultAsync(t => t.Id == transaccionId && t.Cliente.UserId == userId);

                if (transaccion == null)
                {
                    TempData["Error"] = "Comprobante no encontrado o sin acceso.";
                    return RedirectToAction(nameof(EstadoCuenta));
                }

                var vm = new ComprobanteDePagoViewModel
                {
                    NumeroTransaccion = transaccion.NumeroTransaccion,
                    Fecha = transaccion.Fecha,
                    Monto = transaccion.Monto,
                    Metodo = transaccion.MetodoPago,
                    Estado = transaccion.Estado
                };

                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error VerComprobante transaccionId {Id}", transaccionId);
                TempData["Error"] = "Ocurrió un error al cargar el comprobante.";
                return RedirectToAction(nameof(EstadoCuenta));
            }
        }

        // ================== HU6: Reenviar Comprobante (Simulación) ==================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReenviarComprobante(string metodo, int transaccionId)
        {
            try
            {
                var userId = CurrentUserId();

                var transaccion = await _db.Transacciones
                    .Include(t => t.Cliente)
                    .FirstOrDefaultAsync(t => t.Id == transaccionId && t.Cliente.UserId == userId);

                if (transaccion == null)
                {
                    TempData["Error"] = "Comprobante no encontrado.";
                    return RedirectToAction(nameof(EstadoCuenta));
                }

                var cliente = transaccion.Cliente;
                metodo = (metodo ?? "").ToLowerInvariant();

                if (metodo == "email")
                {
                    _logger.LogInformation("[SIMULACIÓN] Email a {Nombre} ({UserId}) | TX {Tx} | Monto S/ {Monto}",
                        cliente?.Nombre, userId, transaccion.NumeroTransaccion, transaccion.Monto);
                    TempData["Success"] = $"Comprobante enviado por correo a {cliente?.Nombre}.";
                }
                else if (metodo == "whatsapp")
                {
                    _logger.LogInformation("[SIMULACIÓN] WhatsApp a {Nombre} ({UserId}) | TX {Tx} | Monto S/ {Monto}",
                        cliente?.Nombre, userId, transaccion.NumeroTransaccion, transaccion.Monto);
                    TempData["Success"] = $"Comprobante enviado por WhatsApp a {cliente?.Nombre}.";
                }
                else
                {
                    TempData["Error"] = "Método de envío no válido.";
                }

                return RedirectToAction(nameof(EstadoCuenta));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reenviando comprobante transaccionId {Id}", transaccionId);
                TempData["Error"] = "Ocurrió un error al reenviar el comprobante.";
                return RedirectToAction(nameof(EstadoCuenta));
            }
        }

        // ================== HU6: Exportar Historial a PDF ==================
        [HttpGet]
        public async Task<IActionResult> ExportarPdf()
        {
            try
            {
                var userId = CurrentUserId();

                var cliente = await _db.Clientes
                    .AsNoTracking()
                    .Include(c => c.Deuda)
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (cliente == null)
                {
                    TempData["Error"] = "Cliente no encontrado.";
                    return RedirectToAction("Index", "Home");
                }

                var historial = await _db.Transacciones
                    .AsNoTracking()
                    .Where(t => t.ClienteId == cliente.Id)
                    .OrderByDescending(t => t.Fecha)
                    .ToListAsync();

                if (!historial.Any())
                {
                    TempData["Warning"] = "No tiene transacciones para exportar.";
                    return RedirectToAction(nameof(EstadoCuenta));
                }

                var html = GeneratePdfContent(cliente, historial);
                var pdfBytes = GeneratePdf(html);

                var safe = string.Join("_", (cliente.Nombre ?? "Cliente").Split(System.IO.Path.GetInvalidFileNameChars()));
                return File(pdfBytes, "application/pdf", $"Historial_{safe}_{DateTime.Now:yyyyMMddHHmm}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ExportarPdf");
                TempData["Error"] = "Ocurrió un error al exportar el historial.";
                return RedirectToAction(nameof(EstadoCuenta));
            }
        }

        // ========== PDF helpers ==========
        private string GeneratePdfContent(Cliente cliente, List<Transaccion> historial)
        {
            var totalTransacciones = historial.Sum(t => t.Monto);

            return $@"
<html>
<head>
<meta charset='utf-8' />
<style>
body {{ font-family: Arial, sans-serif; margin: 30px; }}
.header {{ background-color: #3498db; color: white; padding: 20px; text-align: center; margin-bottom: 30px; border-radius:8px; }}
.info p {{ margin: 0 0 6px 0; }}
table {{ width: 100%; border-collapse: collapse; margin-top: 20px; }}
th {{ background-color: #2c3e50; color: white; padding: 12px; text-align: left; }}
td {{ padding: 10px; border-bottom: 1px solid #ddd; }}
tr:hover {{ background-color: #f5f5f5; }}
.footer {{ margin-top: 30px; text-align: center; color: #7f8c8d; font-size: 12px; }}
.total {{ background-color: #27ae60; color: white; padding: 15px; text-align: center; font-size: 18px; margin-top: 20px; border-radius:8px; }}
</style>
</head>
<body>
    <div class='header'>
        <h1>HISTORIAL DE TRANSACCIONES</h1>
        <p>Sistema AUDICOB - Gestión de Cobranzas</p>
    </div>

    <div class='info'>
        <p><strong>Cliente:</strong> {H(cliente.Nombre)}</p>
        <p><strong>Documento:</strong> {H(cliente.Documento)}</p>
        <p><strong>Generado:</strong> {DateTime.Now:dd/MM/yyyy HH:mm}</p>
        <p><strong>Total de transacciones:</strong> {historial.Count}</p>
    </div>

    <table>
        <thead>
            <tr>
                <th>N° Transacción</th>
                <th>Fecha</th>
                <th>Descripción</th>
                <th>Monto</th>
                <th>Estado</th>
            </tr>
        </thead>
        <tbody>
            {string.Join("", historial.Select(t => $@"
            <tr>
                <td>{H(t.NumeroTransaccion)}</td>
                <td>{t.Fecha:dd/MM/yyyy}</td>
                <td>{H(t.Descripcion)}</td>
                <td>S/ {t.Monto:N2}</td>
                <td>{H(t.Estado)}</td>
            </tr>"))}
        </tbody>
    </table>

    <div class='total'><strong>TOTAL TRANSACCIONES: S/ {totalTransacciones:N2}</strong></div>

    <div class='footer'>
        <p>Documento generado automáticamente por AUDICOB.</p>
        <p>Para consultas, contacte con su asesor de cobranza.</p>
    </div>
</body>
</html>";
        }

        private byte[] GeneratePdf(string htmlContent)
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
                        WebSettings = { DefaultEncoding = "utf-8" }
                    }
                }
            };

            return _pdfConverter.Convert(doc);
        }
    }
}
