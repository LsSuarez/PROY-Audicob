namespace Audicob.Models.ViewModels.Asesor
{
    public class AsesorDashboardViewModel
    {
        // Métricas generales
        public int TotalClientesAsignados { get; set; }
        public decimal TotalDeudaCartera { get; set; }
        public decimal TotalPagosRecientes { get; set; }

        // Gráfica de deudas por cliente
        public List<string> Clientes { get; set; } = new();
        public List<decimal> DeudasPorCliente { get; set; } = new();

        // Detalle de clientes asignados
        public List<ClienteResumen> ClientesAsignados { get; set; } = new();

        // Propiedad para el término de búsqueda en la barra de búsqueda
        public string SearchTerm { get; set; } = string.Empty;
    }

    public class ClienteResumen
    {
        public string Nombre { get; set; } = string.Empty;
        public string Documento { get; set; } = string.Empty;
        public decimal Deuda { get; set; }
        public decimal IngresosMensuales { get; set; }
        public DateTime FechaActualizacion { get; set; }
        public int ClienteId { get; set; }
    }
}
