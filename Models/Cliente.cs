namespace Audicob.Models
{
    public class Cliente
    {
        public int Id { get; set; }
        public string Documento { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public decimal IngresosMensuales { get; set; }
        public decimal DeudaTotal { get; set; }
        public DateTime FechaActualizacion { get; set; } = DateTime.UtcNow;

        // Relación con ApplicationUser
        public string? UserId { get; set; }
        public ApplicationUser? Usuario { get; set; }

        // Relaciones con otras entidades
        public ICollection<Pago> Pagos { get; set; } = new List<Pago>();
        public LineaCredito? LineaCredito { get; set; }
        public ICollection<EvaluacionCliente> Evaluaciones { get; set; } = new List<EvaluacionCliente>();
        public AsignacionAsesor? AsignacionAsesor { get; set; }
        public Deuda? Deuda { get; set; }

        // Método para obtener el total de los pagos realizados en los últimos 30 días
        public decimal PagosRecientes()
        {
            // Definir un rango de 30 días hacia atrás
            var fechaLimite = DateTime.UtcNow.AddMonths(-1);

            // Filtrar los pagos recientes (últimos 30 días) y calcular el total
            var pagosRecientes = Pagos.Where(p => p.Fecha >= fechaLimite)
                                      .Sum(p => p.Monto);
            return pagosRecientes;
        }
    }
}
