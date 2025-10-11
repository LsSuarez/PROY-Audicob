using System;
using System.ComponentModel.DataAnnotations;

namespace Audicob.Models.ViewModels.Cobranza
{
    public class PagoValidacionViewModel
    {
        public int PagoId { get; set; }

        [Display(Name = "Cliente")]
        public string ClienteNombre { get; set; } = string.Empty;

        [Display(Name = "Fecha de Pago")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}")]
        public DateTime Fecha { get; set; }

        [Display(Name = "Monto")]
        [DataType(DataType.Currency)]
        public decimal Monto { get; set; }

        [Display(Name = "Estado Actual")]
        public string Estado { get; set; } = "Pendiente";

        [Display(Name = "Validado")]
        public bool Validado { get; set; }

        public string? Observacion { get; set; }
    }
}
