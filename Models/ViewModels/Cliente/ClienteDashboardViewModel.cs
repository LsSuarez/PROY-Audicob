namespace Audicob.Models.ViewModels.Cliente
{
    public class ClienteDashboardViewModel
    {
        // Datos del cliente
        public string Nombre { get; set; } = string.Empty;
        public decimal DeudaTotal { get; set; }
        public decimal IngresosMensuales { get; set; }

        // Historial de pagos
        public List<string> PagosFechas { get; set; } = new();
        public List<decimal> PagosMontos { get; set; } = new();

        // Últimos pagos
        public List<PagoResumen> PagosRecientes { get; set; } = new();

        // Evaluaciones
        public List<EvaluacionResumen> Evaluaciones { get; set; } = new();
        
        // Asignar Asesor
        public string? Filtro { get; set; }
        public IEnumerable<Audicob.Models.Cliente>? ListCliente { get; set; }
    }

    public class PagoResumen
    {
        public DateTime Fecha { get; set; }
        public decimal Monto { get; set; }
        public bool Validado { get; set; }
        public string Estado => Validado ? "Validado" : "Pendiente";
    }

    public class EvaluacionResumen
    {
        public DateTime Fecha { get; set; }
        public string Estado { get; set; } = string.Empty;
        public string Responsable { get; set; } = string.Empty;
        public string? Comentario { get; set; }
    }
}
