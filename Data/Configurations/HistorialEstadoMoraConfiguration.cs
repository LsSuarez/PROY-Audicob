using Audicob.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Audicob.Data.Configurations
{
    public class HistorialEstadoMoraConfiguration : IEntityTypeConfiguration<HistorialEstadoMora>
    {
        public void Configure(EntityTypeBuilder<HistorialEstadoMora> builder)
        {
            // Configuración de la tabla
            builder.ToTable("HistorialEstadosMora");

            // Configuración de la clave primaria
            builder.HasKey(h => h.Id);
            builder.Property(h => h.Id)
                .ValueGeneratedOnAdd();

            // Configuración de propiedades
            builder.Property(h => h.EstadoAnterior)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(h => h.NuevoEstado)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(h => h.MotivoCambio)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(h => h.Observaciones)
                .HasMaxLength(500);

            builder.Property(h => h.DireccionIP)
                .HasMaxLength(100);

            builder.Property(h => h.UserAgent)
                .HasMaxLength(200);

            builder.Property(h => h.FechaCambio)
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            // Configuración de relaciones
            builder.HasOne(h => h.Cliente)
                .WithMany(c => c.HistorialEstadosMora)
                .HasForeignKey(h => h.ClienteId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            builder.HasOne(h => h.Usuario)
                .WithMany()
                .HasForeignKey(h => h.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            // Índices para mejorar performance de consultas
            builder.HasIndex(h => h.ClienteId)
                .HasDatabaseName("IX_HistorialEstadosMora_ClienteId");

            builder.HasIndex(h => h.FechaCambio)
                .HasDatabaseName("IX_HistorialEstadosMora_FechaCambio");

            builder.HasIndex(h => new { h.ClienteId, h.FechaCambio })
                .HasDatabaseName("IX_HistorialEstadosMora_Cliente_Fecha");
        }
    }
}