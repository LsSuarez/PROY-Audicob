using Audicob.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Audicob.Data.Configurations
{
    public class LineaCreditoConfiguration : IEntityTypeConfiguration<LineaCredito>
    {
        public void Configure(EntityTypeBuilder<LineaCredito> builder)
        {
            builder.ToTable("LineasCredito");

            builder.HasKey(l => l.Id);

            builder.Property(l => l.Monto)
                .HasColumnType("numeric")
                .IsRequired();

            builder.Property(l => l.FechaAsignacion)
                .IsRequired();

            builder.Property(l => l.UsuarioAsignador)
                .IsRequired()
                .HasMaxLength(150);

            builder.HasOne(l => l.Cliente)
                .WithOne(c => c.LineaCredito)
                .HasForeignKey<LineaCredito>(l => l.ClienteId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
