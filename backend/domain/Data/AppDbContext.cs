using Microsoft.EntityFrameworkCore;
using TeachAssist.Domain.Models;

namespace TeachAssist.Domain.Data;

public class DomainDbContext : DbContext
{
    public DomainDbContext(DbContextOptions<DomainDbContext> options) : base(options)
    {
    }

    public DbSet<DomainGroup> Groups => Set<DomainGroup>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Discipline> Disciplines => Set<Discipline>();
    public DbSet<DisciplineTask> Tasks => Set<DisciplineTask>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<DomainGroup>(entity =>
        {
            entity.ToTable("groups");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
            entity.Property(e => e.ShortName).HasColumnName("short_name").HasMaxLength(50).IsRequired();
            entity.Property(e => e.YearStarted).HasColumnName("year_started").IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");
        });

        modelBuilder.Entity<Student>(entity =>
        {
            entity.ToTable("students");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.FirstName).HasColumnName("first_name").HasMaxLength(100).IsRequired();
            entity.Property(e => e.LastName).HasColumnName("last_name").HasMaxLength(100).IsRequired();
            entity.Property(e => e.Email).HasColumnName("email").HasMaxLength(200);
            entity.Property(e => e.GroupId).HasColumnName("group_id").IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");

            entity.HasOne(e => e.Group)
                .WithMany(g => g.Students)
                .HasForeignKey(e => e.GroupId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Discipline>(entity =>
        {
            entity.ToTable("disciplines");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
            entity.Property(e => e.Abbreviation).HasColumnName("abbreviation").HasMaxLength(50).IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");
        });

        modelBuilder.Entity<DisciplineTask>(entity =>
        {
            entity.ToTable("discipline_tasks");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DisciplineId).HasColumnName("discipline_id").IsRequired();
            entity.Property(e => e.Number).HasColumnName("number").IsRequired();
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
            entity.Property(e => e.GradingType).HasColumnName("grading_type").IsRequired();
            entity.Property(e => e.MaxScore).HasColumnName("max_score");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");

            entity.HasOne(e => e.Discipline)
                .WithMany()
                .HasForeignKey(e => e.DisciplineId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
