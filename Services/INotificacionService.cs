using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Audicob.Models;

namespace Audicob.Services
{
    public interface INotificacionService
    {
        Task<List<Notificacion>> ObtenerNotificacionesUsuario(string supervisorId);
        Task<int> ObtenerNotificacionesNoLeidasCount(string supervisorId);
        Task CrearNotificacion(Notificacion notificacion);
        Task MarcarComoLeida(int notificacionId);
        Task EnviarRecordatorios();
    }
}