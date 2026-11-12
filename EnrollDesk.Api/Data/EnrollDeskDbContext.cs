using EnrollDesk.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace EnrollDesk.Api.Data;

public class EnrollDeskDbContext : DbContext
{
    public EnrollDeskDbContext(DbContextOptions<EnrollDeskDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Term> Terms => Set<Term>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<CoursePrerequisite> CoursePrerequisites => Set<CoursePrerequisite>();
    public DbSet<Section> Sections => Set<Section>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.FullName).HasMaxLength(200).IsRequired();
            entity.Property(u => u.Email).HasMaxLength(256).IsRequired();
        });

        modelBuilder.Entity<Term>(entity =>
        {
            entity.HasIndex(t => t.Name).IsUnique();
            entity.Property(t => t.Name).HasMaxLength(20).IsRequired();
        });

        modelBuilder.Entity<Course>(entity =>
        {
            entity.HasIndex(c => c.Code).IsUnique();
            entity.Property(c => c.Code).HasMaxLength(20).IsRequired();
            entity.Property(c => c.Title).HasMaxLength(200).IsRequired();
        });

        modelBuilder.Entity<CoursePrerequisite>(entity =>
        {
            entity.HasKey(cp => new { cp.CourseId, cp.RequiredCourseId });

            entity.HasOne(cp => cp.Course)
                .WithMany(c => c.Prerequisites)
                .HasForeignKey(cp => cp.CourseId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(cp => cp.RequiredCourse)
                .WithMany()
                .HasForeignKey(cp => cp.RequiredCourseId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Section>(entity =>
        {
            entity.Property(s => s.SectionNumber).HasMaxLength(10).IsRequired();
            entity.Property(s => s.InstructorName).HasMaxLength(200);
            entity.Property(s => s.Location).HasMaxLength(100);
            entity.Property(s => s.RowVersion).IsConcurrencyToken();

            entity.HasIndex(s => new { s.CourseId, s.TermId, s.SectionNumber }).IsUnique();

            entity.HasOne(s => s.Course)
                .WithMany(c => c.Sections)
                .HasForeignKey(s => s.CourseId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(s => s.Term)
                .WithMany(t => t.Sections)
                .HasForeignKey(s => s.TermId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Enrollment>(entity =>
        {
            entity.Property(e => e.FinalScore).HasColumnType("decimal(5,2)");

            entity.HasIndex(e => new { e.UserId, e.SectionId }).IsUnique();

            entity.HasOne(e => e.User)
                .WithMany(u => u.Enrollments)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Section)
                .WithMany(s => s.Enrollments)
                .HasForeignKey(e => e.SectionId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
