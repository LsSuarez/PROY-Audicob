using Audicob.Models;
using System;
using System.Collections.Generic;

namespace Audicob.Models.ViewModels.Supervisor
{
    public class InformeFinancieroViewModel
    {
        public int ClienteId { get; set; }
        public string ClienteNombre { get; set; } = string.Empty;
        public string Documento { get; set; } = string.Empty;
        public decimal IngresosMensuales { get; set; }
        public decimal DeudaTotal { get; set; }
        public DateTime FechaActualizacion { get; set; }
        
        public List<Pago> PagosUltimos12Meses { get; set; } = new();
        public decimal TotalPagado12Meses { get; set; }
        
        public LineaCredito? LineaCredito { get; set; }
        public Deuda? Deuda { get; set; }
        public List<EvaluacionCliente> Evaluaciones { get; set; } = new();
    }
}