namespace Audicob.Models.ViewModels.Supervisor
{
    public class SupervisorDashboardViewModel
    {
        public int TotalClientes { get; set; }
        public int EvaluacionesPendientes { get; set; }
        public decimal TotalDeuda { get; set; }
        public decimal TotalPagosUltimoMes { get; set; }

        public List<string> Meses { get; set; } = new();
        public List<decimal> PagosPorMes { get; set; } = new();
        public List<string> Clientes { get; set; } = new();
        public List<decimal> DeudasPorCliente { get; set; } = new();

        // NUEVO: Propiedad para los pagos pendientes
        public List<Pago> PagosPendientes { get; set; } = new();
    }
}
