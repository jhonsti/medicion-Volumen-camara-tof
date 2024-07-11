using BackMineros.Models;
using Microsoft.EntityFrameworkCore;

namespace BackMineros.Data
{
    public class ApplicationDBContext: DbContext 
    {
        public ApplicationDBContext(DbContextOptions<ApplicationDBContext>options) : base(options)
        {
            
        }

        public DbSet<Medicion> Mediciones { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Restricciones para la propiedad Turno
            modelBuilder.Entity<Medicion>()
                .Property(m => m.Turno)
                .IsRequired()
                .HasMaxLength(10); // Definir el tamaño máximo de la cadena según tus necesidades

            modelBuilder.Entity<Medicion>().HasCheckConstraint("CK_Turno", "Turno IN ('Mañana', 'Tarde', 'Noche')");
        }

        public DbSet<ControlCamara> ControlCamara { get; set; }
    }
}
