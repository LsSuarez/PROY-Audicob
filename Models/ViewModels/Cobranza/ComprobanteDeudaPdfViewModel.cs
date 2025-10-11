using System;
using System.ComponentModel.DataAnnotations;

namespace Audicob.Models.ViewModels.Cobranza
{
    public class ComprobanteDeudaPdfViewModel
    {
        [Required]
        public string Cliente { get; set; } = string.Empty;

        [DataType(DataType.Currency)]
        public decimal MontoDeuda { get; set; }

        public int DiasDeAtraso { get; set; }

        /// <summary>0.015 = 1.5% mensual</summary>
        [Range(0, 1)]
        public decimal TasaPenalidad { get; set; }

        [DataType(DataType.Currency)]
        public decimal PenalidadCalculada { get; set; }

        [DataType(DataType.Currency)]
        public decimal TotalAPagar { get; set; }

        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}")]
        public DateTime FechaVencimiento { get; set; }
    }
}
