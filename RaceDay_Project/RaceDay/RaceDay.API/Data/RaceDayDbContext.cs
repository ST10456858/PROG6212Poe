using Microsoft.EntityFrameworkCore;
using RaceDay.API.Models;

namespace RaceDay.API.Data;

public class RaceDayDbContext(DbContextOptions<RaceDayDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Organiser> Organisers => Set<Organiser>();
    public DbSet<Participant> Participants => Set<Participant>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Enrolment> Enrolments => Set<Enrolment>();
    public DbSet<Result> Results => Set<Result>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("Users");
            e.HasKey(x => x.UserId);
            e.Property(x => x.FullName).HasMaxLength(150).IsRequired();
            e.Property(x => x.Email).HasMaxLength(150).IsRequired();
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.PasswordHash).HasMaxLength(255).IsRequired();
            e.Property(x => x.Role).HasMaxLength(20).HasDefaultValue("Participant").IsRequired();
            e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSDATETIME()");
            e.HasCheckConstraint("CK_Users_Role", "[Role] IN ('Organiser', 'Participant')");
        });
        modelBuilder.Entity<Organiser>(e =>
        {
            e.ToTable("Organisers"); e.HasKey(x => x.OrganiserId);
            e.HasIndex(x => x.UserId).IsUnique();
            e.Property(x => x.OrganisationName).HasMaxLength(150);
            e.Property(x => x.ContactNumber).HasMaxLength(20);
            e.HasOne(x => x.User).WithOne(x => x.Organiser).HasForeignKey<Organiser>(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<Participant>(e =>
        {
            e.ToTable("Participants"); e.HasKey(x => x.ParticipantId);
            e.HasIndex(x => x.UserId).IsUnique();
            e.Property(x => x.EmergencyContact).HasMaxLength(20);
            e.HasOne(x => x.User).WithOne(x => x.Participant).HasForeignKey<Participant>(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<Event>(e =>
        {
            e.ToTable("Events"); e.HasKey(x => x.EventId);
            e.Property(x => x.EventName).HasMaxLength(150).IsRequired();
            e.Property(x => x.Description).HasMaxLength(1000);
            e.Property(x => x.Location).HasMaxLength(200).IsRequired();
            e.Property(x => x.RouteMapUrl).HasMaxLength(255);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSDATETIME()");
            e.HasOne(x => x.Organiser).WithMany(x => x.Events).HasForeignKey(x => x.OrganiserId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<Category>(e =>
        {
            e.ToTable("Categories"); e.HasKey(x => x.CategoryId);
            e.Property(x => x.CategoryName).HasMaxLength(100).IsRequired();
            e.Property(x => x.DistanceKm).HasColumnType("decimal(5,2)");
            e.Property(x => x.EntryFee).HasColumnType("decimal(8,2)").HasDefaultValue(0);
            e.HasOne(x => x.Event).WithMany(x => x.Categories).HasForeignKey(x => x.EventId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<Enrolment>(e =>
        {
            e.ToTable("Enrolments"); e.HasKey(x => x.EnrolmentId);
            e.HasIndex(x => new { x.ParticipantId, x.CategoryId }).IsUnique();
            e.Property(x => x.Status).HasMaxLength(20).HasDefaultValue("Confirmed");
            e.Property(x => x.EnrolmentDate).HasDefaultValueSql("SYSDATETIME()");
            e.HasCheckConstraint("CK_Enrolments_Status", "[Status] IN ('Confirmed', 'Cancelled')");
            e.HasOne(x => x.Participant).WithMany(x => x.Enrolments).HasForeignKey(x => x.ParticipantId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.Category).WithMany(x => x.Enrolments).HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<Result>(e =>
        {
            e.ToTable("Results"); e.HasKey(x => x.ResultId);
            e.HasIndex(x => x.EnrolmentId).IsUnique();
            e.Property(x => x.Status).HasMaxLength(20).HasDefaultValue("Finished");
            e.HasCheckConstraint("CK_Results_Status", "[Status] IN ('Finished', 'DNF', 'DNS')");
            e.HasOne(x => x.Enrolment).WithOne(x => x.Result).HasForeignKey<Result>(x => x.EnrolmentId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
