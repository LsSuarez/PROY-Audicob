using System;
using System.ComponentModel.DataAnnotations;

namespace Audicob.Models
{
    public class HistorialCredito
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Nombre del Cliente")]
        public string NombreCliente { get; set; }

        [Required]
        [Display(Name = "DNI del Cliente")]
        public string DniCliente { get; set; }

        [Required]
        [Display(Name = "Código del Cliente")]
        public string CodigoCliente { get; set; }

        [Required]
        [Display(Name = "Tipo de Operación")]
        public string TipoOperacion { get; set; }

        [Required]
        [Display(Name = "Monto de la Operación")]
        [DataType(DataType.Currency)]
        public decimal MontoOperacion { get; set; }

        [Required]
        [Display(Name = "Fecha de la Operación")]
        [DataType(DataType.Date)]
        public DateTime FechaOperacion { get; set; }

        [Required]
        [Display(Name = "Estado de Pago")]
        public string EstadoPago { get; set; }

        [Required]
        [Display(Name = "Producto / Servicio")]
        public string ProductoServicio { get; set; }

        [Required]
        [Display(Name = "Días de Crédito")]
        public int DiasCredito { get; set; }

        [Display(Name = "Observaciones")]
        public string Observaciones { get; set; }
    }
}
