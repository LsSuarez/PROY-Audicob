using System.ComponentModel.DataAnnotations;

namespace Audicob.Models.ViewModels.Asesor
{
    public class RegistrarPagoViewModel
    {
        public int ClienteId { get; set; }
        public string ClienteNombre { get; set; } = string.Empty;
        public decimal DeudaTotal { get; set; }
        public decimal DeudaActual { get; set; }

        [Required(ErrorMessage = "El monto es obligatorio")]
        [Range(0.01, double.MaxValue, ErrorMessage = "El monto debe ser mayor que 0")]
        public decimal Monto { get; set; }

        [Required(ErrorMessage = "El método de pago es obligatorio")]
        public string MetodoPago { get; set; } = string.Empty;

        public string Observaciones { get; set; } = string.Empty;
    }
}