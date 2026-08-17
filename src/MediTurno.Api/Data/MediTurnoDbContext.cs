using MediTurno.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace MediTurno.Api.Data;

public class MediTurnoDbContext(DbContextOptions<MediTurnoDbContext> options) : DbContext(options)
{
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Especialidad> Especialidades => Set<Especialidad>();
    public DbSet<Medico> Medicos => Set<Medico>();
    public DbSet<HorarioAtencion> HorariosAtencion => Set<HorarioAtencion>();
    public DbSet<Paciente> Pacientes => Set<Paciente>();
    public DbSet<Cita> Citas => Set<Cita>();
    public DbSet<Atencion> Atenciones => Set<Atencion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Usuario>(e =>
        {
            e.Property(u => u.NombreCompleto).IsRequired().HasMaxLength(150);
            e.Property(u => u.Correo).IsRequired().HasMaxLength(150);
            e.Property(u => u.PasswordHash).IsRequired();
            e.HasIndex(u => u.Correo).IsUnique();
            e.HasOne(u => u.Medico)
                .WithMany()
                .HasForeignKey(u => u.MedicoId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Especialidad>(e =>
        {
            e.Property(x => x.Nombre).IsRequired().HasMaxLength(100);
            e.HasIndex(x => x.Nombre).IsUnique();
        });

        modelBuilder.Entity<Medico>(e =>
        {
            e.Property(m => m.NombreCompleto).IsRequired().HasMaxLength(150);
            e.Property(m => m.Exequatur).IsRequired().HasMaxLength(50);
            e.HasIndex(m => m.Exequatur).IsUnique();
            e.HasOne(m => m.Especialidad)
                .WithMany(x => x.Medicos)
                .HasForeignKey(m => m.EspecialidadId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<HorarioAtencion>(e =>
        {
            e.HasOne(h => h.Medico)
                .WithMany(m => m.Horarios)
                .HasForeignKey(h => h.MedicoId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(h => new { h.MedicoId, h.DiaSemana });
        });

        modelBuilder.Entity<Paciente>(e =>
        {
            e.Property(p => p.Cedula).IsRequired().HasMaxLength(11);
            e.Property(p => p.Nombre).IsRequired().HasMaxLength(100);
            e.Property(p => p.Apellido).IsRequired().HasMaxLength(100);
            e.Property(p => p.Telefono).HasMaxLength(20);
            e.Property(p => p.Correo).HasMaxLength(150);
            e.HasIndex(p => p.Cedula).IsUnique();
        });

        modelBuilder.Entity<Cita>(e =>
        {
            e.Ignore(c => c.EstaVigente);
            e.Property(c => c.MotivoConsulta).IsRequired().HasMaxLength(300);
            e.Property(c => c.MotivoCancelacion).HasMaxLength(300);
            e.HasOne(c => c.Paciente)
                .WithMany(p => p.Citas)
                .HasForeignKey(c => c.PacienteId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(c => c.Medico)
                .WithMany(m => m.Citas)
                .HasForeignKey(c => c.MedicoId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(c => new { c.MedicoId, c.FechaHora });
        });

        modelBuilder.Entity<Atencion>(e =>
        {
            e.Property(a => a.Motivo).IsRequired().HasMaxLength(300);
            e.Property(a => a.Diagnostico).IsRequired().HasMaxLength(1000);
            e.Property(a => a.Observaciones).HasMaxLength(1000);
            e.HasOne(a => a.Cita)
                .WithOne(c => c.Atencion)
                .HasForeignKey<Atencion>(a => a.CitaId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(a => a.CitaId).IsUnique();
        });
    }
}
