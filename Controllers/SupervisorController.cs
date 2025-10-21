using Audicob.Data;
using Audicob.Models;
using Audicob.Models.ViewModels.Asesor;
using Audicob.Models.ViewModels.Supervisor;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using iTextSharp.text;
using iTextSharp.text.pdf;
using ClosedXML.Excel;
using System.IO;

namespace Audicob.Controllers
{
    [Authorize(Roles = "Supervisor")]
    public class SupervisorController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public SupervisorController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        }

        // Dashboard principal
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

            var pagos = await _db.Pagos
                .Where(p => p.Fecha >= DateTime.UtcNow.AddMonths(-6))
                .GroupBy(p => new { p.Fecha.Year, p.Fecha.Month })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Total = g.Sum(x => x.Monto)
                })
                .OrderBy(x => x.Month)
                .ToListAsync();

            var pagosFormat = pagos.Select(g => new
            {
                Mes = $"{g.Month}/{g.Year}",
                Total = g.Total
            }).ToList();

            vm.Meses = pagosFormat.Select(p => p.Mes).ToList();
            vm.PagosPorMes = pagosFormat.Select(p => p.Total).ToList();

            var deudas = await _db.Clientes
                .OrderByDescending(c => c.DeudaTotal)
                .Take(5)
                .Select(c => new { c.Nombre, c.DeudaTotal })
                .ToListAsync();

            vm.Clientes = deudas.Select(d => d.Nombre).ToList();
            vm.DeudasPorCliente = deudas.Select(d => d.DeudaTotal).ToList();

            var pagosPendientes = await _db.Pagos
                .Where(p => p.Estado == "Pendiente")
                .Include(p => p.Cliente)
                .OrderBy(p => p.Fecha)
                .Take(10)
                .ToListAsync();

            vm.PagosPendientes = pagosPendientes;

            return View(vm);
        }

        // HU7: Validar pago
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ValidarPago(int pagoId)
        {
            var pago = await _db.Pagos
                .Include(p => p.Cliente)
                .FirstOrDefaultAsync(p => p.Id == pagoId);
                
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
            
            var fechaValidacion = DateTime.UtcNow;
            pago.Observacion = $"Validado por {user.FullName} el {fechaValidacion:dd/MM/yyyy HH:mm:ss}";

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

        // GET: Asignar línea de crédito (HU3)
        public async Task<IActionResult> AsignarLineaCredito()
        {
            var clientes = await _db.Clientes
                .Select(c => new { c.Id, c.Nombre })
                .ToListAsync();

            Console.WriteLine("Clientes encontrados (GET): " + clientes.Count);

            ViewBag.Clientes = clientes;
            return View();
        }

        // POST: Asignar línea de crédito (HU3)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AsignarLineaCredito(int clienteId, decimal monto)
        {
            if (!ModelState.IsValid)
            {
                var clientes = await _db.Clientes
                    .Select(c => new { c.Id, c.Nombre })
                    .ToListAsync();
                ViewBag.Clientes = clientes;
                return View();
            }

            if (monto < 180)
            { 
                TempData["Error"] = "Debe ingresar un monto mayor a 180.";
                return RedirectToAction("AsignarLineaCredito");
            }

            var cliente = await _db.Clientes
                .Include(c => c.LineaCredito)
                .FirstOrDefaultAsync(c => c.Id == clienteId);
            if (cliente == null)
            {
                TempData["Error"] = "Cliente no válido.";
                return RedirectToAction("AsignarLineaCredito");
            }

            if (cliente.LineaCredito != null)
            {
                TempData["Error"] = "El cliente ya tiene asignada una línea de crédito.";
                return RedirectToAction("AsignarLineaCredito");
            }

            var linea = new LineaCredito
            {
                ClienteId = cliente.Id,
                Monto = monto,
                FechaAsignacion = DateTime.UtcNow,
                UsuarioAsignador = User.Identity?.Name ?? "Supervisor"
            };

            _db.LineasCredito.Add(linea);
            await _db.SaveChangesAsync();

            TempData["Success"] = $"Línea de crédito asignada a {cliente.Nombre}.";
            return RedirectToAction("AsignarLineaCredito");
        }
        // HU1: Ver informe financiero detallado
        public async Task<IActionResult> VerInformeFinanciero(int id)
        {
            var cliente = await _db.Clientes
                .Include(c => c.Pagos)
                .Include(c => c.Deuda)
                .Include(c => c.Evaluaciones)
                .Include(c => c.LineaCredito)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (cliente == null)
            {
                TempData["Error"] = "Cliente no encontrado.";
                return RedirectToAction("Dashboard");
            }

            var pagosUltimos12Meses = await _db.Pagos
                .Where(p => p.ClienteId == cliente.Id && p.Fecha >= DateTime.UtcNow.AddMonths(-12))
                .OrderByDescending(p => p.Fecha)
                .ToListAsync();

            var vm = new InformeFinancieroViewModel
            {
                ClienteId = cliente.Id,
                ClienteNombre = cliente.Nombre,
                Documento = cliente.Documento,
                IngresosMensuales = cliente.IngresosMensuales,
                DeudaTotal = cliente.DeudaTotal,
                FechaActualizacion = cliente.FechaActualizacion,
                PagosUltimos12Meses = pagosUltimos12Meses,
                TotalPagado12Meses = pagosUltimos12Meses.Sum(p => p.Monto),
                LineaCredito = cliente.LineaCredito,
                Deuda = cliente.Deuda,
                Evaluaciones = cliente.Evaluaciones.OrderByDescending(e => e.Fecha).ToList()
            };

            return View(vm);
        }

        // HU2: Ver evaluaciones pendientes
        public async Task<IActionResult> EvaluacionesPendientes()
        {
            var evaluaciones = await _db.Evaluaciones
                .Include(e => e.Cliente)
                .Where(e => e.Estado == "Pendiente")
                .OrderBy(e => e.Fecha)
                .ToListAsync();

            return View(evaluaciones);
        }

        // HU2: Ver detalle de evaluación
        public async Task<IActionResult> DetalleEvaluacion(int id)
        {
            var evaluacion = await _db.Evaluaciones
                .Include(e => e.Cliente)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (evaluacion == null)
            {
                TempData["Error"] = "Evaluación no encontrada.";
                return RedirectToAction("EvaluacionesPendientes");
            }

            var vm = new EvaluacionViewModel
            {
                ClienteId = evaluacion.ClienteId,
                NombreCliente = evaluacion.Cliente.Nombre,
                IngresosMensuales = evaluacion.Cliente.IngresosMensuales,
                DeudaTotal = evaluacion.Cliente.DeudaTotal,
                Estado = evaluacion.Estado,
                Responsable = evaluacion.Responsable,
                Comentario = evaluacion.Comentario,
                FechaEvaluacion = evaluacion.Fecha
            };

            return View(vm);
        }

        // HU2: Confirmar evaluación
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmarEvaluacion(int id)
        {
            var evaluacion = await _db.Evaluaciones
                .Include(e => e.Cliente)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (evaluacion == null)
            {
                TempData["Error"] = "Evaluación no encontrada.";
                return RedirectToAction("EvaluacionesPendientes");
            }

            var user = await _userManager.GetUserAsync(User);

            evaluacion.Estado = "Marcado";
            evaluacion.Responsable = user.FullName;
            evaluacion.Fecha = DateTime.UtcNow;

            _db.Update(evaluacion);
            await _db.SaveChangesAsync();

            TempData["Success"] = $"Evaluación confirmada y marcada por {user.FullName}.";
            return RedirectToAction("EvaluacionesPendientes");
        }

        // HU2: Rechazar evaluación
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RechazarEvaluacion(int id, string comentario)
        {
            if (string.IsNullOrWhiteSpace(comentario))
            {
                TempData["Error"] = "El comentario es obligatorio al rechazar una evaluación.";
                return RedirectToAction("DetalleEvaluacion", new { id });
            }

            var evaluacion = await _db.Evaluaciones
                .Include(e => e.Cliente)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (evaluacion == null)
            {
                TempData["Error"] = "Evaluación no encontrada.";
                return RedirectToAction("EvaluacionesPendientes");
            }

            var user = await _userManager.GetUserAsync(User);

            evaluacion.Estado = "Rechazado";
            evaluacion.Responsable = user.FullName;
            evaluacion.Comentario = comentario;
            evaluacion.Fecha = DateTime.UtcNow;

            _db.Update(evaluacion);
            await _db.SaveChangesAsync();

            TempData["Success"] = $"Evaluación rechazada por {user.FullName}.";
            return RedirectToAction("EvaluacionesPendientes");
        }

        // HU4: Buscar cliente
        public async Task<IActionResult> BuscarCliente(string codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo))
            {
                TempData["Error"] = "Debe ingresar un código o documento para buscar.";
                return RedirectToAction("Dashboard");
            }

            var cliente = await _db.Clientes
                .Include(c => c.Pagos)
                .Include(c => c.Deuda)
                .Include(c => c.Evaluaciones)
                .Include(c => c.LineaCredito)
                .Include(c => c.AsignacionAsesor)
                .FirstOrDefaultAsync(c => c.Documento == codigo || c.Id.ToString() == codigo);

            if (cliente == null)
            {
                TempData["Error"] = "Cliente no encontrado.";
                return RedirectToAction("Dashboard");
            }

            return RedirectToAction("PerfilCliente", new { id = cliente.Id });
        }

        // HU4: Ver perfil completo del cliente
        public async Task<IActionResult> PerfilCliente(int id)
        {
            var cliente = await _db.Clientes
                .Include(c => c.Pagos)
                .Include(c => c.Deuda)
                .Include(c => c.Evaluaciones)
                .Include(c => c.LineaCredito)
                .Include(c => c.AsignacionAsesor)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (cliente == null)
            {
                TempData["Error"] = "Cliente no encontrado.";
                return RedirectToAction("Dashboard");
            }

            var transacciones = await _db.Transacciones
                .Where(t => t.ClienteId == cliente.Id)
                .OrderByDescending(t => t.Fecha)
                .Take(10)
                .ToListAsync();

            var vm = new PerfilClienteViewModel
            {
                ClienteInfo = cliente,  // CORREGIDO: Cambié "Cliente" a "ClienteInfo"
                TransaccionesRecientes = transacciones,
                TotalPagos = cliente.Pagos.Sum(p => p.Monto),
                PagosValidados = cliente.Pagos.Count(p => p.Validado),
                PagosPendientes = cliente.Pagos.Count(p => !p.Validado)
            };

            return View(vm);
        }

        private async Task<Cliente?> TryGetClienteAsync(int clienteId)
        {
            return await _db.Clientes
                .Include(c => c.LineaCredito)
                .FirstOrDefaultAsync(c => c.Id == clienteId);
        }
        //HU11: Gestionar asignación de asesores
        // GET: Mostrar la lista de asesores asignados
        public async Task<IActionResult> GestionAsignacion()
        {
            var asesores = await _db.AsesoresAsignados.ToListAsync();
            return View(asesores);
        }

        // POST: Reportar la asignación (copiar los datos y registrar en la tabla ReportesAsignacion)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReportarAsignacion()
        {
            var asesores = await _db.AsesoresAsignados.ToListAsync();

            if (!asesores.Any())
            {
                TempData["Error"] = "No hay datos para reportar.";
                return RedirectToAction("GestionAsignacion");
            }

            var user = await _userManager.GetUserAsync(User);

            foreach (var a in asesores)
            {
                var reporte = new ReporteAsignacion
                {
                    AsesorNombre = a.AsesorNombre,
                    CantidadCarteras = a.CantidadCarteras,
                    MontoTotal = a.MontoTotal,
                    CantidadCuentas = a.CantidadCuentas,
                    Estado = a.Estado,
                    Responsable = user.FullName ?? user.UserName,
                    FechaRegistro = DateTime.UtcNow
                };

                _db.ReportesAsignacion.Add(reporte);
            }

            await _db.SaveChangesAsync();

            TempData["Success"] = "Asignaciones reportadas exitosamente.";
            return RedirectToAction("GestionAsignacion");
        }
        // HU11 Ver reportes anteriores
        public async Task<IActionResult> ReportesAnteriores()
        {
            var reportes = await _db.ReportesAsignacion
                .OrderByDescending(r => r.FechaRegistro)
                .ToListAsync();

            return PartialView("_ReportesAnteriores", reportes);
        }
        // HU11: Exportar reportes a PDF y Excel
        public async Task<IActionResult> ExportarPDF()
        {
            var asesores = await _db.AsesoresAsignados.ToListAsync();

            using (var memoryStream = new MemoryStream())
            {
                Document doc = new Document(PageSize.A4);
                PdfWriter.GetInstance(doc, memoryStream);
                doc.Open();

                Paragraph title = new Paragraph("Reporte de Asignación de Carteras\n\n")
                {
                    Alignment = Element.ALIGN_CENTER
                };
                doc.Add(title);

                PdfPTable table = new PdfPTable(5);
                table.WidthPercentage = 100;
                table.AddCell("Asesor");
                table.AddCell("Cantidad Carteras");
                table.AddCell("Monto Total");
                table.AddCell("Cantidad Cuentas");
                table.AddCell("Estado");

                foreach (var a in asesores)
                {
                    table.AddCell(a.AsesorNombre);
                    table.AddCell(a.CantidadCarteras.ToString());
                    table.AddCell(a.MontoTotal.ToString("C"));
                    table.AddCell(a.CantidadCuentas.ToString());
                    table.AddCell(a.Estado);
                }

                doc.Add(table);
                doc.Close();

                byte[] bytes = memoryStream.ToArray();
                return File(bytes, "application/pdf", "ReporteAsignacion.pdf");
            }
        }

        public async Task<IActionResult> ExportarExcel()
        {
            var asesores = await _db.AsesoresAsignados.ToListAsync();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Asignaciones");
                worksheet.Cell(1, 1).Value = "Asesor";
                worksheet.Cell(1, 2).Value = "Cantidad Carteras";
                worksheet.Cell(1, 3).Value = "Monto Total";
                worksheet.Cell(1, 4).Value = "Cantidad Cuentas";
                worksheet.Cell(1, 5).Value = "Estado";

                int row = 2;
                foreach (var a in asesores)
                {
                    worksheet.Cell(row, 1).Value = a.AsesorNombre;
                    worksheet.Cell(row, 2).Value = a.CantidadCarteras;
                    worksheet.Cell(row, 3).Value = a.MontoTotal;
                    worksheet.Cell(row, 4).Value = a.CantidadCuentas;
                    worksheet.Cell(row, 5).Value = a.Estado;
                    row++;
                }

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "ReporteAsignacion.xlsx");
                }
            }
        }
    }
}