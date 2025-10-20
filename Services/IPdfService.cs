using Audicob.Models;

namespace Audicob.Services
{
    public interface IPdfService
    {
        byte[] GenerarPdfNotificaciones(List<Notificacion> notificaciones, string nombreUsuario);
    }
}