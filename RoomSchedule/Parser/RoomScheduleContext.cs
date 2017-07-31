using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Parser
{
    public partial class RoomScheduleContext : DbContext
    {
        public virtual DbSet<Class> Class { get; set; }
        public virtual DbSet<Semester> Semester { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            #warning To protect potentially sensitive information in your connection string, you should move it out of source code. See http://go.microsoft.com/fwlink/?LinkId=723263 for guidance on storing connection strings.
            optionsBuilder.UseSqlServer(@"Server=THE-ONE;Database=RoomSchedule;Trusted_Connection=True;");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Class>(entity =>
            {
                entity.HasKey(e => e.Syn)
                    .HasName("Class_pk");

                entity.Property(e => e.Syn)
                    .HasColumnName("SYN")
                    .ValueGeneratedNever();

                entity.Property(e => e.Begin).HasColumnType("time(0)");

                entity.Property(e => e.Building)
                    .IsRequired()
                    .HasColumnType("varchar(20)");

                entity.Property(e => e.Days)
                    .IsRequired()
                    .HasColumnType("varchar(10)");

                entity.Property(e => e.End).HasColumnType("time(0)");

                entity.Property(e => e.Professor)
                    .IsRequired()
                    .HasColumnType("varchar(30)");

                entity.Property(e => e.Room)
                    .IsRequired()
                    .HasColumnType("varchar(10)");

                entity.Property(e => e.Section)
                    .IsRequired()
                    .HasColumnType("varchar(50)");

                entity.Property(e => e.Semester)
                    .IsRequired()
                    .HasColumnType("varchar(30)");

                entity.Property(e => e.Title)
                    .IsRequired()
                    .HasColumnType("varchar(100)");

                entity.HasOne(d => d.SemesterNavigation)
                    .WithMany(p => p.Class)
                    .HasForeignKey(d => d.Semester)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("Class_Semesters");
            });

            modelBuilder.Entity<Semester>(entity =>
            {
                entity.HasKey(e => e.Name)
                    .HasName("Semester_pk");

                entity.Property(e => e.Name).HasColumnType("varchar(30)");
            });
        }
    }
}