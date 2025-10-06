namespace Audicob.Models.ViewModels.Supervisor
{
    public class SegmentacionViewModel
    {
        public string Segmento { get; set; } = string.Empty; // Ej: "Alto riesgo", "Bajo riesgo"
        public decimal MinDeuda { get; set; }
        public decimal MaxDeuda { get; set; }
        public decimal MinIngresos { get; set; }
        public decimal MaxIngresos { get; set; }

        public List<ClienteSegmentado> ClientesSegmentados { get; set; } = new();
    }

    public class ClienteSegmentado
    {
        public string Nombre { get; set; } = string.Empty;
        public decimal DeudaTotal { get; set; }
        public decimal IngresosMensuales { get; set; }
    }
}
