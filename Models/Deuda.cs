using System;
using System.ComponentModel.DataAnnotations;

namespace Audicob.Models
{
    public class Deuda
    {
        public int Id { get; set; }

        // Monto original de la deuda, nunca debe ser nulo, por eso se inicializa con un valor válido.
        [Range(0.01, double.MaxValue, ErrorMessage = "El monto debe ser mayor que 0.")]
        public decimal Monto { get; set; }

        // Intereses generados por atraso
        public decimal Intereses { get; set; }  // Propiedad de intereses

        // Penalidad calculada, esta propiedad se calculará en base a la deuda.
        public decimal PenalidadCalculada { get; set; }

        // Total a pagar, es el monto más la penalidad calculada.
        public decimal TotalAPagar { get; set; }

        // Fecha de vencimiento de la deuda, nunca debe ser nula
        public DateTime FechaVencimiento { get; set; }

        // Clasificación de la deuda: Preventiva, Judicial, Extrajudicial
        [Required(ErrorMessage = "La clasificación es obligatoria.")]
        public string Clasificacion { get; set; } = "Preventiva";

        // Relación con Cliente: un cliente tiene una deuda, por lo tanto se requiere
        public int ClienteId { get; set; }

        // Cliente no puede ser nulo, y es obligatorio asociar un cliente a la deuda
        [Required(ErrorMessage = "El cliente es obligatorio.")]
        public Cliente Cliente { get; set; } = new Cliente(); // Se inicializa para evitar problemas con nulabilidad

        // Método para recalcular la penalidad y el total a pagar basado en la fecha actual y los días de atraso
        public void CalcularPenalidad(DateTime fechaActual, decimal tasaPenalidadMensual)
        {
            var diasDeAtraso = (fechaActual - FechaVencimiento).Days;

            // Si hay atraso, se calcula la penalidad
            if (diasDeAtraso > 0)
            {
                // Penalidad calculada
                PenalidadCalculada = Monto * tasaPenalidadMensual * diasDeAtraso / 30; // Penalidad mensual
                Intereses = PenalidadCalculada; // Los intereses son los mismos que la penalidad
                TotalAPagar = Monto + Intereses; // Total a pagar es el monto más los intereses
            }
            else
            {
                Intereses = 0;  // Si no hay atraso, los intereses son 0
                PenalidadCalculada = 0; // No se genera penalidad
                TotalAPagar = Monto;  // Solo se paga el monto original
            }
        }
    }
}
