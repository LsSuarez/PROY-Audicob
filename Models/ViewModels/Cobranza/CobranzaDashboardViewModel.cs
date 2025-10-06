namespace Audicob.Models.ViewModels.Cobranza
{
    public class CobranzaDashboardViewModel
    {
        // Total de clientes asignados al asesor
        public int TotalClientesAsignados { get; set; }

        // Término de búsqueda utilizado por el asesor
        public string SearchTerm { get; set; }

        // Total de la deuda en la cartera del asesor
        public decimal TotalDeudaCartera { get; set; }

        // Lista de clientes asignados con sus respectivas deudas
        public List<ClienteDeudaViewModel> Clientes { get; set; } = new List<ClienteDeudaViewModel>();

        // Total de usuarios que pertenecen al rol del asesor (opcional)
        public int TotalUsuarios { get; set; }

        // Lista de los roles asignados al usuario
        public List<string> UsuariosPorRol { get; set; } = new List<string>();

        // Propiedad para mostrar el mensaje de "No hay resultados"
        public bool NoResultados { get; set; }

        // Constructor que asegura que las listas estén inicializadas
        public CobranzaDashboardViewModel()
        {
            Clientes = new List<ClienteDeudaViewModel>();
            UsuariosPorRol = new List<string>();
        }

        // Método auxiliar para calcular el porcentaje de deuda de la cartera
        public decimal GetPorcentajeDeudaCartera(decimal totalDeuda)
        {
            if (totalDeuda == 0) return 0;
            return (TotalDeudaCartera / totalDeuda) * 100;
        }

        // Método auxiliar para verificar si hay clientes en la búsqueda
        public void VerificarResultadosBusqueda()
        {
            NoResultados = Clientes == null || !Clientes.Any();
        }
    }
}
