using Audicob.Data;
using Audicob.Models;
using Audicob.Models.ViewModels.Cobranza;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using DinkToPdf;
using DinkToPdf.Contracts;

namespace Audicob.Controllers
{
    [Authorize(Roles = "AsesorCobranza")]
    public class CobranzaController : Controller
    {
        private readonly ApplicationDbContext _db;

        public CobranzaController(ApplicationDbContext db)
        {
            _db = db;
        }

        // 1. Dashboard de Cobranza con búsqueda
        public async Task<IActionResult> Dashboard(string searchTerm = "")
        {
            try
            {
                var userId = User.Identity.Name;

                var asignaciones = await _db.AsignacionesAsesores
                    .Include(a => a.Clientes)
                    .ThenInclude(c => c.Deuda)
                    .Where(a => a.AsesorUserId == userId)
                    .ToListAsync();

                var clientes = asignaciones.SelectMany(a => a.Clientes).ToList();

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    clientes = clientes.Where(c =>
                        c.Nombre.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                        c.Documento.Contains(searchTerm)).ToList();
                }

                var vm = new CobranzaDashboardViewModel
                {
                    SearchTerm = searchTerm,
                    TotalClientesAsignados = clientes.Count,
                    TotalDeudaCartera = clientes.Sum(c => c.Deuda?.TotalAPagar ?? 0),
                    Clientes = clientes.Select(c => new ClienteDeudaViewModel
                    {
                        ClienteId = c.Id,
                        ClienteNombre = c.Nombre,
                        DeudaTotal = c.Deuda?.TotalAPagar ?? 0
                    }).ToList()
                };

                vm.VerificarResultadosBusqueda();

                return View(vm);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Ocurrió un error al cargar el dashboard: " + ex.Message;
                return RedirectToAction("Index", "Home");
            }
        }

        // 2. Consultar Deuda Detallada
        public async Task<IActionResult> ConsultarDeuda(int clienteId)
        {
            try
            {
                var cliente = await _db.Clientes
                    .Include(c => c.Deuda)
                    .FirstOrDefaultAsync(c => c.Id == clienteId);

                if (cliente == null || cliente.Deuda == null)
                {
                    TempData["Error"] = "Cliente o deuda no encontrada.";
                    return RedirectToAction("Dashboard");
                }

                var deuda = cliente.Deuda;
                var diasDeAtraso = (DateTime.Now - deuda.FechaVencimiento).Days;
                if (diasDeAtraso < 0) diasDeAtraso = 0; // No puede ser negativo

                var penalidadCalculada = CalcularPenalidad(deuda.Monto, diasDeAtraso);

                var model = new DeudaDetalleViewModel
                {
                    Cliente = cliente.Nombre,
                    MontoDeuda = deuda.Monto,
                    DiasAtraso = diasDeAtraso,
                    PenalidadCalculada = penalidadCalculada,
                    TotalAPagar = deuda.Monto + penalidadCalculada,
                    FechaVencimiento = deuda.FechaVencimiento,
                    TasaPenalidad = 0.015m
                };

                return View(model);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al consultar la deuda: " + ex.Message;
                return RedirectToAction("Dashboard");
            }
        }

        // 3. Actualizar Penalidad (Actualización automática en tiempo real)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActualizarPenalidad(int clienteId)
        {
            try
            {
                var cliente = await _db.Clientes
                    .Include(c => c.Deuda)
                    .FirstOrDefaultAsync(c => c.Id == clienteId);

                if (cliente == null || cliente.Deuda == null)
                {
                    TempData["Error"] = "Cliente o deuda no encontrada.";
                    return RedirectToAction("Dashboard");
                }

                var deuda = cliente.Deuda;
                var diasDeAtraso = (DateTime.Now - deuda.FechaVencimiento).Days;
                if (diasDeAtraso < 0) diasDeAtraso = 0;

                var penalidadCalculada = CalcularPenalidad(deuda.Monto, diasDeAtraso);

                // Actualizar penalidad e intereses
                deuda.PenalidadCalculada = penalidadCalculada;
                deuda.Intereses = penalidadCalculada;
                deuda.TotalAPagar = deuda.Monto + penalidadCalculada;

                _db.Update(deuda);
                await _db.SaveChangesAsync();

                TempData["Success"] = $"Penalidad actualizada correctamente. Nueva penalidad: S/ {penalidadCalculada:N2}";
                return RedirectToAction("ConsultarDeuda", new { clienteId });
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al actualizar la penalidad: " + ex.Message;
                return RedirectToAction("ConsultarDeuda", new { clienteId });
            }
        }

        // 4. Ver detalles calculados paso a paso (HU9)
        public async Task<IActionResult> VerDetallesCalculados(int clienteId)
        {
            try
            {
                var cliente = await _db.Clientes
                    .Include(c => c.Deuda)
                    .FirstOrDefaultAsync(c => c.Id == clienteId);

                if (cliente == null || cliente.Deuda == null)
                {
                    TempData["Error"] = "Cliente o deuda no encontrada.";
                    return RedirectToAction("Dashboard");
                }

                var deuda = cliente.Deuda;
                var diasDeAtraso = (DateTime.Now - deuda.FechaVencimiento).Days;
                if (diasDeAtraso < 0) diasDeAtraso = 0;

                var tasaMensual = 0.015m; // 1.5% mensual
                var tasaDiaria = tasaMensual / 30;
                var penalidadCalculada = deuda.Monto * tasaDiaria * diasDeAtraso;

                var model = new CalculoPenalidadDetalleViewModel
                {
                    ClienteNombre = cliente.Nombre,
                    MontoOriginal = deuda.Monto,
                    FechaVencimiento = deuda.FechaVencimiento,
                    DiasDeAtraso = diasDeAtraso,
                    TasaPenalidadMensual = tasaMensual,
                    TasaPenalidadDiaria = tasaDiaria,
                    PenalidadCalculada = penalidadCalculada,
                    TotalAPagar = deuda.Monto + penalidadCalculada,

                    // Fórmula paso a paso
                    FormulaTexto = "Penalidad = Monto Original × Tasa Diaria × Días de Atraso",
                    Paso1 = $"Tasa Mensual = {tasaMensual:P2} (1.5%)",
                    Paso2 = $"Tasa Diaria = {tasaMensual:P4} ÷ 30 días = {tasaDiaria:P4}",
                    Paso3 = $"Penalidad = S/ {deuda.Monto:N2} × {tasaDiaria:P4} × {diasDeAtraso} días",
                    Paso4 = $"Penalidad = S/ {penalidadCalculada:N2}"
                };

                ViewBag.ClienteId = clienteId;
                return View(model);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al calcular los detalles: " + ex.Message;
                return RedirectToAction("Dashboard");
            }
        }

        // 5. Generar Comprobante PDF (HU9)
        public async Task<IActionResult> GenerarComprobante(int clienteId)
        {
            try
            {
                var cliente = await _db.Clientes
                    .Include(c => c.Deuda)
                    .FirstOrDefaultAsync(c => c.Id == clienteId);

                if (cliente == null || cliente.Deuda == null)
                {
                    TempData["Error"] = "Cliente o deuda no encontrada.";
                    return RedirectToAction("Dashboard");
                }

                var deuda = cliente.Deuda;
                var diasDeAtraso = (DateTime.Now - deuda.FechaVencimiento).Days;
                if (diasDeAtraso < 0) diasDeAtraso = 0;

                var penalidadCalculada = CalcularPenalidad(deuda.Monto, diasDeAtraso);

                var model = new ComprobanteDeudaViewModel
                {
                    Cliente = cliente.Nombre,
                    MontoDeuda = deuda.Monto,
                    DiasDeAtraso = diasDeAtraso,
                    TasaPenalidad = 0.015m,
                    PenalidadCalculada = penalidadCalculada,
                    TotalAPagar = deuda.Monto + penalidadCalculada,
                    FechaVencimiento = deuda.FechaVencimiento
                };

                var htmlContent = GenerateHtml(model);
                var pdfBytes = GeneratePdf(htmlContent);

                return File(pdfBytes, "application/pdf", $"Comprobante_{cliente.Nombre}_{DateTime.Now:yyyyMMdd}.pdf");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al generar el comprobante: " + ex.Message;
                return RedirectToAction("ConsultarDeuda", new { clienteId });
            }
        }

        // Calcular Penalidad
        private decimal CalcularPenalidad(decimal monto, int diasDeAtraso)
        {
            if (diasDeAtraso <= 0) return 0;

            decimal tasaPenalidadMensual = 0.015m; // 1.5% mensual
            decimal tasaPenalidadDiaria = tasaPenalidadMensual / 30;
            return monto * tasaPenalidadDiaria * diasDeAtraso;
        }

        // Generar HTML mejorado para el PDF
        private string GenerateHtml(ComprobanteDeudaViewModel model)
        {
            return $@"
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; margin: 40px; }}
                        h1 {{ color: #2c3e50; text-align: center; }}
                        .header {{ background-color: #3498db; color: white; padding: 20px; text-align: center; }}
                        .content {{ margin: 20px 0; }}
                        .row {{ display: flex; justify-content: space-between; margin: 10px 0; }}
                        .label {{ font-weight: bold; }}
                        .value {{ text-align: right; }}
                        .total {{ background-color: #e74c3c; color: white; padding: 15px; font-size: 20px; text-align: center; margin-top: 20px; }}
                        table {{ width: 100%; border-collapse: collapse; margin: 20px 0; }}
                        td {{ padding: 10px; border-bottom: 1px solid #ddd; }}
                    </style>
                </head>
                <body>
                    <div class='header'>
                        <h1>COMPROBANTE DE DEUDA</h1>
                        <p>Sistema de Cobranza AUDICOB</p>
                    </div>
                    <div class='content'>
                        <p><strong>Fecha de emisión:</strong> {DateTime.Now:dd/MM/yyyy HH:mm}</p>
                        
                        <table>
                            <tr>
                                <td class='label'>Cliente:</td>
                                <td class='value'>{model.Cliente}</td>
                            </tr>
                            <tr>
                                <td class='label'>Monto Original:</td>
                                <td class='value'>S/ {model.MontoDeuda:N2}</td>
                            </tr>
                            <tr>
                                <td class='label'>Fecha de Vencimiento:</td>
                                <td class='value'>{model.FechaVencimiento:dd/MM/yyyy}</td>
                            </tr>
                            <tr>
                                <td class='label'>Días de Atraso:</td>
                                <td class='value'>{model.DiasDeAtraso} días</td>
                            </tr>
                            <tr>
                                <td class='label'>Tasa de Penalidad Mensual:</td>
                                <td class='value'>{model.TasaPenalidad:P2}</td>
                            </tr>
                            <tr>
                                <td class='label'>Penalidad Calculada:</td>
                                <td class='value' style='color: #e74c3c;'>S/ {model.PenalidadCalculada:N2}</td>
                            </tr>
                        </table>
                        
                        <div class='total'>
                            <strong>TOTAL A PAGAR: S/ {model.TotalAPagar:N2}</strong>
                        </div>
                    </div>
                </body>
                </html>
            ";
        }

        // Generar PDF usando DinkToPdf
        private byte[] GeneratePdf(string htmlContent)
        {
            var converter = new BasicConverter(new PdfTools());
            var doc = new HtmlToPdfDocument()
            {
                GlobalSettings = {
                    ColorMode = ColorMode.Color,
                    Orientation = Orientation.Portrait,
                    PaperSize = PaperKind.A4,
                    Margins = new MarginSettings { Top = 10, Bottom = 10, Left = 10, Right = 10 }
                },
                Objects = {
                    new ObjectSettings() {
                        HtmlContent = htmlContent,
                        WebSettings = { DefaultEncoding = "utf-8" }
                    }
                }
            };

            return converter.Convert(doc);
        }
    }
}