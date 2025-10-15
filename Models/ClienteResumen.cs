public class ClienteResumen
{
    public int ClienteId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Documento { get; set; } = string.Empty;
    public decimal Deuda { get; set; }
    public decimal IngresosMensuales { get; set; }
    public DateTime FechaActualizacion { get; set; }
}
