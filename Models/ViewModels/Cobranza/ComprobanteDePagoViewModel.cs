using System;
using System.ComponentModel.DataAnnotations;

namespace Audicob.Models.ViewModels.Cobranza
{
    public class ComprobanteDePagoViewModel
    {
        [Required]
        public string NumeroTransaccion { get; set; } = string.Empty;

        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy HH:mm}")]
        public DateTime Fecha { get; set; }

        [DataType(DataType.Currency)]
        public decimal Monto { get; set; }

        [Required]
        public string Metodo { get; set; } = string.Empty;

        [Required]
        public string Estado { get; set; } = string.Empty;
    }
}
