namespace Audicob.Models.ViewModels.Supervisor
{
    public class AsignacionLineaCreditoViewModel
    {
        public int ClienteId { get; set; }
        public string NombreCliente { get; set; } = string.Empty;
        public decimal DeudaTotal { get; set; }
        public decimal IngresosMensuales { get; set; }

        public decimal MontoAsignado { get; set; }
        public string UsuarioAsignador { get; set; } = string.Empty;
        public DateTime FechaAsignacion { get; set; } = DateTime.UtcNow;
    }
}
