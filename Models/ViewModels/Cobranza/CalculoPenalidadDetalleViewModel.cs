// Models/ViewModels/Cobranza/CalculoPenalidadDetalleViewModel.cs
using System;
using System.ComponentModel.DataAnnotations;

namespace Audicob.Models.ViewModels.Cobranza
{
    public class CalculoPenalidadDetalleViewModel
    {
        public string ClienteNombre { get; set; } = string.Empty;

        [DataType(DataType.Currency)]
        public decimal MontoOriginal { get; set; }

        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}")]
        public DateTime FechaVencimiento { get; set; }

        public int DiasDeAtraso { get; set; }

        public decimal TasaPenalidadMensual { get; set; }
        public decimal TasaPenalidadDiaria { get; set; }

        [DataType(DataType.Currency)]
        public decimal PenalidadCalculada { get; set; }

        [DataType(DataType.Currency)]
        public decimal TotalAPagar { get; set; }

        // Paso a paso
        public string FormulaTexto { get; set; } = string.Empty;
        public string Paso1 { get; set; } = string.Empty;
        public string Paso2 { get; set; } = string.Empty;
        public string Paso3 { get; set; } = string.Empty;
        public string Paso4 { get; set; } = string.Empty;
    }
}
