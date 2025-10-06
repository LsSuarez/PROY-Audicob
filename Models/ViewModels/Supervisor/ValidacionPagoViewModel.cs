namespace Audicob.Models.ViewModels.Supervisor
{
    public class ValidacionPagoViewModel
    {
        public int PagoId { get; set; }
        public int ClienteId { get; set; }
        public string NombreCliente { get; set; } = string.Empty;

        public DateTime FechaPago { get; set; }
        public decimal Monto { get; set; }
        public bool Validado { get; set; }
        public string? Observacion { get; set; }
    }
}
