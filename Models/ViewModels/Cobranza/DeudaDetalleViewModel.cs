using System;
using System.ComponentModel.DataAnnotations;

namespace Audicob.Models.ViewModels.Cobranza
{
    public class DeudaDetalleViewModel
    {
        public int ClienteId { get; set; }                 // ← usado por botones

        public string Cliente { get; set; } = string.Empty;

        [DataType(DataType.Currency)]
        public decimal MontoDeuda { get; set; }

        public int DiasAtraso { get; set; }

        /// <summary>Ej. 0.015 para 1.5% mensual</summary>
        public decimal TasaPenalidad { get; set; }

        [DataType(DataType.Currency)]
        public decimal PenalidadCalculada { get; set; }

        [DataType(DataType.Currency)]
        public decimal TotalAPagar { get; set; }

        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}")]
        public DateTime FechaVencimiento { get; set; }
    }
}
