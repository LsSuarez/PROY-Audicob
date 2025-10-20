using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Audicob.Services
{
    public class RecordatorioHostedService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<RecordatorioHostedService> _logger;
        private readonly TimeSpan _intervalo = TimeSpan.FromHours(1); // Ejecutar cada hora

        public RecordatorioHostedService(IServiceProvider serviceProvider,
            ILogger<RecordatorioHostedService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Servicio de Recordatorios iniciado");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var notificacionService = scope.ServiceProvider
                            .GetRequiredService<INotificacionService>();
                        await notificacionService.EnviarRecordatorios();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error en el servicio de recordatorios");
                }

                await Task.Delay(_intervalo, stoppingToken);
            }
        }
    }
}