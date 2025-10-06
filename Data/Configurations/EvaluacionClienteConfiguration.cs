using Audicob.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Audicob.Data.Configurations
{
    public class EvaluacionClienteConfiguration : IEntityTypeConfiguration<EvaluacionCliente>
    {
        public void Configure(EntityTypeBuilder<EvaluacionCliente> builder)
        {
            builder.ToTable("Evaluaciones");

            builder.HasKey(e => e.Id);

            builder.Property(e => e.Estado)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(e => e.Comentario)
                .HasMaxLength(500);

            builder.Property(e => e.Responsable)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(e => e.Fecha)
                .IsRequired();

            builder.HasOne(e => e.Cliente)
                .WithMany(c => c.Evaluaciones)
                .HasForeignKey(e => e.ClienteId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
