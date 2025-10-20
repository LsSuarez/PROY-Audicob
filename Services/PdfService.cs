using Audicob.Helpers;
using Audicob.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QuestPDF.Elements;


namespace Audicob.Services
{
    public class PdfService : IPdfService
    {
        public byte[] GenerarPdfNotificaciones(List<Notificacion> notificaciones, string nombreUsuario)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var documento = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    // ========== ENCABEZADO ==========
                    page.Header()
                        .AlignCenter()
                        .Column(column =>
                        {
                            column.Item().Text("AUDICOB ERP")
                                .FontSize(20)
                                .Bold()
                                .FontColor(Colors.Blue.Darken2);

                            column.Item().Text("Reporte de Notificaciones")
                                .FontSize(14)
                                .SemiBold()
                                .FontColor(Colors.Grey.Darken2);

                            column.Item().PaddingTop(5).Text($"Usuario: {nombreUsuario}")
                                .FontSize(10)
                                .FontColor(Colors.Grey.Darken1);

                            column.Item().Text($"Fecha de generación: {DateTimeHelper.GetPeruTime():dd/MM/yyyy HH:mm}")
                                .FontSize(10)
                                .FontColor(Colors.Grey.Darken1);

                            column.Item().PaddingVertical(10).Element(e =>
                            {
                                e.BorderBottom(1).BorderColor(Colors.Grey.Lighten2);
                            });
                        });

                    // ========== CONTENIDO ==========
                    page.Content()
                        .PaddingVertical(10)
                        .Column(column =>
                        {
                            var noLeidas = notificaciones.Count(n => !n.Leida);
                            var leidas = notificaciones.Count - noLeidas;

                            column.Item().PaddingBottom(15).Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                });

                                // Encabezados
                                table.Cell().Background(Colors.Blue.Lighten3).Padding(8)
                                    .Text("Total").Bold().FontSize(11);
                                table.Cell().Background(Colors.Green.Lighten3).Padding(8)
                                    .Text("Leídas").Bold().FontSize(11);
                                table.Cell().Background(Colors.Red.Lighten3).Padding(8)
                                    .Text("No Leídas").Bold().FontSize(11);

                                // Valores
                                table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8)
                                    .AlignCenter().Text(notificaciones.Count.ToString()).FontSize(16).Bold();
                                table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8)
                                    .AlignCenter().Text(leidas.ToString()).FontSize(16).Bold().FontColor(Colors.Green.Medium);
                                table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8)
                                    .AlignCenter().Text(noLeidas.ToString()).FontSize(16).Bold().FontColor(Colors.Red.Medium);
                            });

                            // LISTA DE NOTIFICACIONES
                            if (!notificaciones.Any())
                            {
                                column.Item().AlignCenter().PaddingVertical(30).Text("No hay notificaciones para mostrar")
                                    .FontSize(12)
                                    .Italic()
                                    .FontColor(Colors.Grey.Medium);
                            }
                            else
                            {
                                column.Item().PaddingTop(5).PaddingBottom(10).Text("Detalle de Notificaciones")
                                    .FontSize(12)
                                    .Bold()
                                    .FontColor(Colors.Grey.Darken2);

                                foreach (var notificacion in notificaciones)
                                {
                                    column.Item().PaddingBottom(10).Border(1)
                                        .BorderColor(notificacion.Leida ? Colors.Grey.Lighten2 : Colors.Blue.Medium)
                                        .Background(notificacion.Leida ? Colors.White : Colors.Blue.Lighten4)
                                        .Padding(12)
                                        .Column(notifColumn =>
                                        {
                                            // TÍTULO Y ESTADO
                                            notifColumn.Item().Row(row =>
                                            {
                                                row.RelativeItem().Text(text =>
                                                {
                                                    text.Span($"{notificacion.IconoTipo ?? "📋"} ")
                                                        .FontSize(14);
                                                    text.Span(notificacion.Titulo)
                                                        .FontSize(12)
                                                        .Bold()
                                                        .FontColor(Colors.Blue.Darken2);
                                                });

                                                if (!notificacion.Leida)
                                                {
                                                    row.ConstantItem(70).AlignRight()
                                                        .Background(Colors.Red.Medium)
                                                        .Padding(4)
                                                        .Text("NO LEÍDA")
                                                        .FontSize(8)
                                                        .Bold()
                                                        .FontColor(Colors.White);
                                                }
                                                else
                                                {
                                                    row.ConstantItem(50).AlignRight()
                                                        .Background(Colors.Green.Medium)
                                                        .Padding(4)
                                                        .Text("LEÍDA")
                                                        .FontSize(8)
                                                        .Bold()
                                                        .FontColor(Colors.White);
                                                }
                                            });

                                            // LÍNEA SEPARADORA
                                            notifColumn.Item().PaddingVertical(5).Element(e =>
                                            {
                                                e.BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten1);
                                            });

                                            // DESCRIPCIÓN
                                            notifColumn.Item().PaddingTop(3).Text(notificacion.Descripcion)
                                                .FontSize(10)
                                                .FontColor(Colors.Black)
                                                .LineHeight(1.3f);

                                            // CLIENTE (si existe)
                                            if (notificacion.Cliente != null)
                                            {
                                                notifColumn.Item().PaddingTop(5).Row(row =>
                                                {
                                                    row.ConstantItem(60).Text("Cliente:")
                                                        .FontSize(9)
                                                        .SemiBold()
                                                        .FontColor(Colors.Grey.Darken1);
                                                    row.RelativeItem().Text(notificacion.Cliente.Nombre)
                                                        .FontSize(9)
                                                        .FontColor(Colors.Grey.Darken2);
                                                });
                                            }

                                            // TIPO DE NOTIFICACIÓN
                                            notifColumn.Item().PaddingTop(3).Row(row =>
                                            {
                                                row.ConstantItem(60).Text("Tipo:")
                                                    .FontSize(9)
                                                    .SemiBold()
                                                    .FontColor(Colors.Grey.Darken1);
                                                row.RelativeItem().Text(notificacion.TipoNotificacion)
                                                    .FontSize(9)
                                                    .Italic()
                                                    .FontColor(Colors.Grey.Medium);
                                            });

                                            // FECHAS
                                            notifColumn.Item().PaddingTop(8).Background(Colors.Grey.Lighten4)
                                                .Padding(6)
                                                .Row(row =>
                                                {
                                                    row.RelativeItem().Column(col =>
                                                    {
                                                        col.Item().Text("Fecha de creación")
                                                            .FontSize(8)
                                                            .SemiBold()
                                                            .FontColor(Colors.Grey.Darken1);
                                                        col.Item().Text(DateTimeHelper.ConvertToPeruTime(notificacion.FechaCreacion).ToString("dd/MM/yyyy HH:mm"))
                                                            .FontSize(9)
                                                            .FontColor(Colors.Blue.Medium);
                                                    });

                                                    if (notificacion.Leida && notificacion.FechaLectura.HasValue)
                                                    {
                                                        row.RelativeItem().Column(col =>
                                                        {
                                                            col.Item().Text("Fecha de lectura")
                                                                .FontSize(8)
                                                                .SemiBold()
                                                                .FontColor(Colors.Grey.Darken1);
                                                            col.Item().Text(DateTimeHelper.ConvertToPeruTime(notificacion.FechaLectura.Value).ToString("dd/MM/yyyy HH:mm"))
                                                                .FontSize(9)
                                                                .FontColor(Colors.Green.Medium);
                                                        });
                                                    }
                                                });
                                        });
                                }
                            }
                        });

                    // ========== PIE DE PÁGINA ==========
                    page.Footer()
                        .AlignCenter()
                        .Column(column =>
                        {
                            column.Item().PaddingVertical(5).Element(e =>
                            {
                                e.BorderBottom(1).BorderColor(Colors.Grey.Lighten2);
                            });

                            column.Item().PaddingTop(5).Row(row =>
                            {
                                row.RelativeItem().AlignLeft()
                                    .Text($"AUDICOB ERP - {DateTimeHelper.GetPeruTime():yyyy}")
                                    .FontSize(8)
                                    .FontColor(Colors.Grey.Medium);

                                row.RelativeItem().AlignRight()
                                    .Text("Página 1")
                                    .FontSize(8)
                                    .FontColor(Colors.Grey.Medium);
                            });
                        });

                });
            });

            return documento.GeneratePdf();
        }
    }
}