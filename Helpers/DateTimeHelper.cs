using System;

namespace Audicob.Helpers
{
    public static class DateTimeHelper
    {
        // Zona horaria de Perú (UTC-5)
        private static readonly TimeZoneInfo PeruTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time");

        /// <summary>
        /// Obtiene la fecha y hora actual en la zona horaria de Perú
        /// </summary>
        public static DateTime GetPeruTime()
        {
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, PeruTimeZone);
        }

        /// <summary>
        /// Convierte una fecha UTC a la zona horaria de Perú
        /// </summary>
        public static DateTime ConvertToPeruTime(DateTime utcDateTime)
        {
            if (utcDateTime.Kind != DateTimeKind.Utc)
            {
                utcDateTime = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
            }
            return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, PeruTimeZone);
        }

        /// <summary>
        /// Convierte una fecha de Perú a UTC para guardar en la base de datos
        /// </summary>
        public static DateTime ConvertToUtc(DateTime peruDateTime)
        {
            return TimeZoneInfo.ConvertTimeToUtc(peruDateTime, PeruTimeZone);
        }
    }
}