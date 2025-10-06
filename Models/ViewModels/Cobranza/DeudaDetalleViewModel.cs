namespace Audicob.Models.ViewModels.Cobranza
{
    public class DeudaDetalleViewModel
    {
        public string Cliente { get; set; }
        public decimal MontoDeuda { get; set; }
        public int DiasAtraso { get; set; }
        public decimal TasaPenalidad { get; set; }
        public decimal PenalidadCalculada { get; set; }
        public decimal TotalAPagar { get; set; }
        public DateTime FechaVencimiento { get; set; } // Asegúrate de que esta propiedad esté presente
    }
}
