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
using System.Threading.Tasks;

namespace Audicob.Controllers
{
    [Authorize(Roles = "Cliente")]
    public class AbonoController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<AbonoController> _logger;

        public AbonoController(ApplicationDbContext db, ILogger<AbonoController> logger)
        {
            _db = db;
            _logger = logger;
        }

        // Ver estado de cuenta (HU6)
        public async Task<IActionResult> EstadoCuenta()
        {
            var userId = User.Identity.Name;

            var cliente = await _db.Clientes
                .Where(c => c.UserId == userId)
                .Include(c => c.Deuda)
                .FirstOrDefaultAsync();

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

            var estadoCuenta = new EstadoCuentaViewModel
            {
                TotalDeuda = cliente.Deuda.TotalAPagar,
                Capital = cliente.Deuda.Monto,
                Intereses = cliente.Deuda.PenalidadCalculada,
                HistorialTransacciones = await _db.Transacciones
                    .Where(t => t.ClienteId == cliente.Id)
                    .OrderByDescending(t => t.Fecha)
                    .ToListAsync()
            };

            return View(estadoCuenta);
        }

        // Filtrar historial (HU6)
        public async Task<IActionResult> FiltrarHistorial(string searchTerm, decimal? montoMin, decimal? montoMax, DateTime? fechaDesde, DateTime? fechaHasta)
        {
            var userId = User.Identity.Name;

            var cliente = await _db.Clientes
                .Where(c => c.UserId == userId)
                .FirstOrDefaultAsync();

            if (cliente == null)
            {
                return Json(new { error = "Cliente no encontrado" });
            }

            var transacciones = _db.Transacciones
                .Where(t => t.ClienteId == cliente.Id)
                .AsQueryable();

            // Filtrar por descripción
            if (!string.IsNullOrEmpty(searchTerm))
                transacciones = transacciones.Where(t => t.Descripcion.Contains(searchTerm) || 
                                                         t.NumeroTransaccion.Contains(searchTerm));

            // Filtrar por monto mínimo
            if (montoMin.HasValue)
                transacciones = transacciones.Where(t => t.Monto >= montoMin);

            // Filtrar por monto máximo
            if (montoMax.HasValue)
                transacciones = transacciones.Where(t => t.Monto <= montoMax);

            // Filtrar por rango de fechas
            if (fechaDesde.HasValue)
                transacciones = transacciones.Where(t => t.Fecha >= fechaDesde.Value);

            if (fechaHasta.HasValue)
                transacciones = transacciones.Where(t => t.Fecha <= fechaHasta.Value);

            var historial = await transacciones
                .OrderByDescending(t => t.Fecha)
                .ToListAsync();

            return PartialView("_HistorialTransacciones", historial);
        }

        // Ver detalle del comprobante de pago (HU6)
        public async Task<IActionResult> VerComprobante(int transaccionId)
        {
            var userId = User.Identity.Name;
            
            var transaccion = await _db.Transacciones
                .Include(t => t.Cliente)
                .Where(t => t.Id == transaccionId && t.Cliente.UserId == userId)
                .FirstOrDefaultAsync();

            if (transaccion == null)
            {
                TempData["Error"] = "Comprobante no encontrado o no tiene acceso a él.";
                return RedirectToAction("EstadoCuenta");
            }

            var model = new ComprobanteDePagoViewModel
            {
                NumeroTransaccion = transaccion.NumeroTransaccion,
                Fecha = transaccion.Fecha,
                Monto = transaccion.Monto,
                Metodo = transaccion.MetodoPago,
                Estado = transaccion.Estado
            };

            return View(model);
        }

        // Reenviar comprobante por email o WhatsApp (HU6 - Simulación)
        public async Task<IActionResult> ReenviarComprobante(string metodo, int transaccionId)
        {
            var userId = User.Identity.Name;

            var transaccion = await _db.Transacciones
                .Include(t => t.Cliente)
                .FirstOrDefaultAsync(t => t.Id == transaccionId && t.Cliente.UserId == userId);

            if (transaccion == null)
            {
                TempData["Error"] = "Comprobante no encontrado.";
                return RedirectToAction("EstadoCuenta");
            }

            var cliente = transaccion.Cliente;

            if (metodo == "email")
            {
                // Simulación de envío por correo electrónico
                _logger.LogInformation($"[SIMULACIÓN] Email enviado a {cliente.Nombre} ({User.Identity.Name})");
                _logger.LogInformation($"Contenido: Comprobante #{transaccion.NumeroTransaccion} - Monto: S/ {transaccion.Monto:N2}");
                
                TempData["Success"] = $"Comprobante de pago enviado por correo electrónico a {cliente.Nombre}.";
            }
            else if (metodo == "whatsapp")
            {
                // Simulación de envío por WhatsApp
                _logger.LogInformation($"[SIMULACIÓN] WhatsApp enviado a {cliente.Nombre} ({User.Identity.Name})");
                _logger.LogInformation($"Mensaje: Su comprobante #{transaccion.NumeroTransaccion} por S/ {transaccion.Monto:N2} está disponible");
                
                TempData["Success"] = $"Comprobante de pago enviado por WhatsApp a {cliente.Nombre}.";
            }
            else
            {
                TempData["Error"] = "Método de envío no válido.";
            }

            return RedirectToAction("EstadoCuenta");
        }

        // Exportar historial de transacciones a PDF (HU6)
        public async Task<IActionResult> ExportarPdf()
        {
            var userId = User.Identity.Name;

            var cliente = await _db.Clientes
                .Where(c => c.UserId == userId)
                .Include(c => c.Deuda)
                .FirstOrDefaultAsync();

            if (cliente == null)
            {
                TempData["Error"] = "Cliente no encontrado.";
                return RedirectToAction("Index", "Home");
            }

            var historial = await _db.Transacciones
                .Where(t => t.ClienteId == cliente.Id)
                .OrderByDescending(t => t.Fecha)
                .ToListAsync();

            if (!historial.Any())
            {
                TempData["Warning"] = "No tiene transacciones para exportar.";
                return RedirectToAction("EstadoCuenta");
            }

            var pdfContent = GeneratePdfContent(cliente, historial);
            var pdfBytes = GeneratePdf(pdfContent);

            return File(pdfBytes, "application/pdf", $"Historial_{cliente.Nombre}_{DateTime.Now:yyyyMMdd}.pdf");
        }

        // Generar contenido HTML mejorado para el PDF
        private string GeneratePdfContent(Cliente cliente, List<Transaccion> historial)
        {
            var totalTransacciones = historial.Sum(t => t.Monto);
            
            var content = $@"
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; margin: 30px; }}
                        .header {{ background-color: #3498db; color: white; padding: 20px; text-align: center; margin-bottom: 30px; }}
                        .info {{ margin: 20px 0; }}
                        table {{ width: 100%; border-collapse: collapse; margin-top: 20px; }}
                        th {{ background-color: #2c3e50; color: white; padding: 12px; text-align: left; }}
                        td {{ padding: 10px; border-bottom: 1px solid #ddd; }}
                        tr:hover {{ background-color: #f5f5f5; }}
                        .footer {{ margin-top: 30px; text-align: center; color: #7f8c8d; font-size: 12px; }}
                        .total {{ background-color: #27ae60; color: white; padding: 15px; text-align: center; font-size: 18px; margin-top: 20px; }}
                    </style>
                </head>
                <body>
                    <div class='header'>
                        <h1>HISTORIAL DE TRANSACCIONES</h1>
                        <p>Sistema AUDICOB - Gestión de Cobranzas</p>
                    </div>
                    
                    <div class='info'>
                        <p><strong>Cliente:</strong> {cliente.Nombre}</p>
                        <p><strong>Documento:</strong> {cliente.Documento}</p>
                        <p><strong>Fecha de generación:</strong> {DateTime.Now:dd/MM/yyyy HH:mm}</p>
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
                        <tbody>";

            foreach (var trans in historial)
            {
                content += $@"
                            <tr>
                                <td>{trans.NumeroTransaccion}</td>
                                <td>{trans.Fecha:dd/MM/yyyy}</td>
                                <td>{trans.Descripcion}</td>
                                <td>S/ {trans.Monto:N2}</td>
                                <td>{trans.Estado}</td>
                            </tr>";
            }

            content += $@"
                        </tbody>
                    </table>
                    
                    <div class='total'>
                        <strong>TOTAL TRANSACCIONES: S/ {totalTransacciones:N2}</strong>
                    </div>
                    
                    <div class='footer'>
                        <p>Este documento es un resumen generado automáticamente por el sistema AUDICOB.</p>
                        <p>Para consultas o aclaraciones, contacte con su asesor de cobranza.</p>
                    </div>
                </body>
                </html>";

            return content;
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