using Audicob.Models;

namespace Audicob.Models.ViewModels.Supervisor
{
    public class FiltroMoraViewModel
    {
        // Criterios de filtrado
        public int? RangoDiasDesde { get; set; }
        public int? RangoDiasHasta { get; set; }
        public string? TipoCliente { get; set; }
        public decimal? MontoDesde { get; set; }
        public decimal? MontoHasta { get; set; }
        public string? EstadoMora { get; set; }
        
        // Filtros guardados
        public string? NombreFiltroGuardado { get; set; }
        public bool GuardarFiltro { get; set; }
        public List<FiltroGuardado> FiltrosGuardados { get; set; } = new List<FiltroGuardado>();
        
        // Resultados
        public List<ClienteMoraInfo> ResultadosFiltrados { get; set; } = new List<ClienteMoraInfo>();
        
        // Metadatos de resultados
        public int TotalRegistros { get; set; }
        public TimeSpan TiempoRespuesta { get; set; }
        public DateTime FechaConsulta { get; set; } = DateTime.UtcNow;
    }

    public class ClienteMoraInfo
    {
        public int ClienteId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Documento { get; set; } = string.Empty;
        public decimal DeudaTotal { get; set; }
        public int DiasEnMora { get; set; }
        public decimal MontoEnMora { get; set; }
        public string TipoCliente { get; set; } = string.Empty;
        public string EstadoMora { get; set; } = string.Empty;
        public DateTime? FechaUltimoPago { get; set; }
        public string? AsesorAsignado { get; set; }
        public decimal IngresosMensuales { get; set; }
        
        // Campos calculados para priorización
        public string NivelPrioridad { get; set; } = "Media";
        public string ColorIndicador { get; set; } = "warning";
        
        public void CalcularPrioridad()
        {
            if (DiasEnMora >= 90 || MontoEnMora >= 5000)
            {
                NivelPrioridad = "Crítica";
                ColorIndicador = "danger";
            }
            else if (DiasEnMora >= 60 || MontoEnMora >= 3000)
            {
                NivelPrioridad = "Alta";
                ColorIndicador = "warning";
            }
            else if (DiasEnMora >= 30 || MontoEnMora >= 1000)
            {
                NivelPrioridad = "Media";
                ColorIndicador = "info";
            }
            else
            {
                NivelPrioridad = "Baja";
                ColorIndicador = "secondary";
            }
        }
    }


}