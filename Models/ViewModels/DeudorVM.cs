namespace Audicob.Models.ViewModels
{
    public enum NivelCriticidad { Bajo = 0, Medio = 1, Alto = 2, Critico = 3 }

    public class DeudorVM
    {
        public int DeudaId { get; set; }
        public int ClienteId { get; set; }
        public string Cliente { get; set; } = "";
        public decimal Monto { get; set; }
        public int AntiguedadDias { get; set; }
        public NivelCriticidad Criticidad { get; set; }
    }
}
