using System;
using System.Collections.Generic;

namespace Audicob.Models.ViewModels.Cobranza
{
    public class ClientesDniSearchViewModel
    {
        public string? Q { get; set; }  // DNI (o parte)
        public List<Item> Items { get; set; } = new();

        public class Item
        {
            public int ClienteId { get; set; }
            public string Nombre { get; set; } = string.Empty;
            public string DNI { get; set; } = string.Empty;
            public int? DeudaId { get; set; }
            public decimal Monto { get; set; }
            public DateTime? FechaVencimiento { get; set; }
            public int DiasAtraso { get; set; }
            public decimal Penalidad { get; set; }
            public decimal Total { get; set; }
        }
    }
}
