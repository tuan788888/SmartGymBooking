using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using SmartGymBooking.Models;

namespace SmartGymBooking.Data;

public partial class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Aiconversation> Aiconversations { get; set; }

    public virtual DbSet<Aimessage> Aimessages { get; set; }

    public virtual DbSet<Booking> Bookings { get; set; }

    public virtual DbSet<CheckIn> CheckIns { get; set; }

    public virtual DbSet<Customer> Customers { get; set; }

    public virtual DbSet<Employee> Employees { get; set; }

    public virtual DbSet<Exercise> Exercises { get; set; }

    public virtual DbSet<Membership> Memberships { get; set; }

    public virtual DbSet<Package> Packages { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<Pt> Pts { get; set; }

    public virtual DbSet<Ptschedule> Ptschedules { get; set; }

    public virtual DbSet<Service> Services { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<WorkoutExercise> WorkoutExercises { get; set; }

    public virtual DbSet<WorkoutPlan> WorkoutPlans { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Aiconversation>(entity =>
        {
            entity.HasKey(e => e.AiconversationId).HasName("PK__AIConver__6604C7F2EC07B8C2");

            entity.ToTable("AIConversations");

            entity.HasIndex(e => e.CustomerId, "IX_AIConversations_CustomerId");

            entity.Property(e => e.AiconversationId).HasColumnName("AIConversationId");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Title).HasMaxLength(200);

            entity.HasOne(d => d.Customer).WithMany(p => p.Aiconversations)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AIConversations_Customers");
        });

        modelBuilder.Entity<Aimessage>(entity =>
        {
            entity.HasKey(e => e.AimessageId).HasName("PK__AIMessag__E8DCEE036F280AF8");

            entity.ToTable("AIMessages");

            entity.HasIndex(e => new { e.AiconversationId, e.CreatedAt }, "IX_AIMessages_Conversation_CreatedAt");

            entity.Property(e => e.AimessageId).HasColumnName("AIMessageId");
            entity.Property(e => e.AiconversationId).HasColumnName("AIConversationId");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Role).HasMaxLength(20);

            entity.HasOne(d => d.Aiconversation).WithMany(p => p.Aimessages)
                .HasForeignKey(d => d.AiconversationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AIMessages_AIConversations");
        });

        modelBuilder.Entity<Booking>(entity =>
        {
            entity.HasKey(e => e.BookingId).HasName("PK__Bookings__73951AED9D42DD0C");

            entity.HasIndex(e => e.CustomerId, "IX_Bookings_CustomerId");

            entity.HasIndex(e => new { e.CustomerId, e.StartTime, e.EndTime }, "IX_Bookings_Customer_Time");

            entity.HasIndex(e => e.ServiceId, "IX_Bookings_ServiceId");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Note).HasMaxLength(500);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("CONFIRMED");

            entity.HasOne(d => d.Customer).WithMany(p => p.Bookings)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Bookings_Customers");

            entity.HasOne(d => d.Service).WithMany(p => p.Bookings)
                .HasForeignKey(d => d.ServiceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Bookings_Services");
        });

        modelBuilder.Entity<CheckIn>(entity =>
        {
            entity.HasKey(e => e.CheckInId).HasName("PK__CheckIns__E6497684567EC63C");

            entity.HasIndex(e => e.CustomerId, "IX_CheckIns_CustomerId");

            entity.HasIndex(e => e.BookingId, "UX_CheckIns_BookingId")
                .IsUnique()
                .HasFilter("([BookingId] IS NOT NULL)");

            entity.Property(e => e.CheckInTime).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Note).HasMaxLength(500);

            entity.HasOne(d => d.Booking).WithOne(p => p.CheckIn)
                .HasForeignKey<CheckIn>(d => d.BookingId)
                .HasConstraintName("FK_CheckIns_Bookings");

            entity.HasOne(d => d.Customer).WithMany(p => p.CheckIns)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CheckIns_Customers");
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.CustomerId).HasName("PK__Customer__A4AE64D856DC6D7E");

            entity.HasIndex(e => e.UserId, "UQ_Customers_UserId").IsUnique();

            entity.HasIndex(e => e.Phone, "UX_Customers_Phone")
                .IsUnique()
                .HasFilter("([Phone] IS NOT NULL)");

            entity.Property(e => e.ActivityLevel).HasMaxLength(20);
            entity.Property(e => e.Avatar).HasMaxLength(1000);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.ExperienceLevel).HasMaxLength(50);
            entity.Property(e => e.FitnessGoal).HasMaxLength(100);
            entity.Property(e => e.FullName).HasMaxLength(150);
            entity.Property(e => e.Gender).HasMaxLength(20);
            entity.Property(e => e.Height).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.Weight).HasColumnType("decimal(5, 2)");

            entity.HasOne(d => d.User).WithOne(p => p.Customer)
                .HasForeignKey<Customer>(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Customers_Users");
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasKey(e => e.EmployeeId).HasName("PK__Employee__7AD04F1183B9A7C5");

            entity.HasIndex(e => e.UserId, "UQ_Employees_UserId").IsUnique();

            entity.HasIndex(e => e.Phone, "UX_Employees_Phone")
                .IsUnique()
                .HasFilter("([Phone] IS NOT NULL)");

            entity.Property(e => e.Avatar).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())", "DF_Employees_CreatedAt");
            entity.Property(e => e.FullName).HasMaxLength(150);
            entity.Property(e => e.Gender).HasMaxLength(20);
            entity.Property(e => e.IsActive).HasDefaultValue(true, "DF_Employees_IsActive");
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.Position).HasMaxLength(100);

            entity.HasOne(d => d.User).WithOne(p => p.Employee)
                .HasForeignKey<Employee>(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Employees_Users");
        });

        modelBuilder.Entity<Exercise>(entity =>
        {
            entity.HasKey(e => e.ExerciseId).HasName("PK__Exercise__A074AD2F1F37B92F");

            entity.HasIndex(e => e.Name, "UQ_Exercises_Name").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Difficulty).HasMaxLength(50);
            entity.Property(e => e.ImageUrl).HasMaxLength(1000);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.MuscleGroup).HasMaxLength(100);
            entity.Property(e => e.Name).HasMaxLength(150);
            entity.Property(e => e.VideoUrl).HasMaxLength(1000);
        });

        modelBuilder.Entity<Membership>(entity =>
        {
            entity.HasKey(e => e.MembershipId).HasName("PK__Membersh__92A78679B413A255");

            entity.HasIndex(e => e.CustomerId, "IX_Memberships_CustomerId");

            entity.HasIndex(e => new { e.CustomerId, e.Status }, "IX_Memberships_Customer_Status");

            entity.HasIndex(e => e.PackageId, "IX_Memberships_PackageId");

            entity.HasIndex(e => e.PaymentId, "UQ_Memberships_PaymentId").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("ACTIVE");

            entity.HasOne(d => d.Customer).WithMany(p => p.Memberships)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Memberships_Customers");

            entity.HasOne(d => d.Package).WithMany(p => p.Memberships)
                .HasForeignKey(d => d.PackageId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Memberships_Packages");

            entity.HasOne(d => d.Payment).WithOne(p => p.Membership)
                .HasForeignKey<Membership>(d => d.PaymentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Memberships_Payments");
        });

        modelBuilder.Entity<Package>(entity =>
        {
            entity.HasKey(e => e.PackageId).HasName("PK__Packages__322035CCBC712C82");

            entity.HasIndex(e => e.Name, "UQ_Packages_Name").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.IncludesPt).HasColumnName("IncludesPT");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Name).HasMaxLength(150);
            entity.Property(e => e.Price).HasColumnType("decimal(18, 2)");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.PaymentId).HasName("PK__Payments__9B556A38319C3CC1");

            entity.HasIndex(e => e.CustomerId, "IX_Payments_CustomerId");

            entity.HasIndex(e => e.PackageId, "IX_Payments_PackageId");

            entity.HasIndex(e => e.Status, "IX_Payments_Status");

            entity.HasIndex(e => e.PaymentCode, "UQ_Payments_PaymentCode").IsUnique();

            entity.Property(e => e.Amount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.PaymentCode).HasMaxLength(50);
            entity.Property(e => e.PaymentMethod)
                .HasMaxLength(50)
                .HasDefaultValue("QR_BANK_TRANSFER");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("PENDING");

            entity.HasOne(d => d.Customer).WithMany(p => p.Payments)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Payments_Customers");

            entity.HasOne(d => d.Package).WithMany(p => p.Payments)
                .HasForeignKey(d => d.PackageId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Payments_Packages");
        });

        modelBuilder.Entity<Pt>(entity =>
        {
            entity.HasKey(e => e.Ptid).HasName("PK__PTs__BCC07F6F58FA33AF");

            entity.ToTable("PTs");

            entity.HasIndex(e => e.Email, "UX_PTs_Email")
                .IsUnique()
                .HasFilter("([Email] IS NOT NULL)");

            entity.HasIndex(e => e.Phone, "UX_PTs_Phone")
                .IsUnique()
                .HasFilter("([Phone] IS NOT NULL)");

            entity.Property(e => e.Ptid).HasColumnName("PTId");
            entity.Property(e => e.Avatar).HasMaxLength(1000);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.FullName).HasMaxLength(150);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.Specialization).HasMaxLength(150);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("ACTIVE");
        });

        modelBuilder.Entity<Ptschedule>(entity =>
        {
            entity.HasKey(e => e.PtscheduleId).HasName("PK__PTSchedu__D9A744959089B307");

            entity.ToTable("PTSchedules");

            entity.HasIndex(e => e.CustomerId, "IX_PTSchedules_CustomerId");

            entity.HasIndex(e => new { e.CustomerId, e.StartTime, e.EndTime }, "IX_PTSchedules_Customer_Time");

            entity.HasIndex(e => e.Ptid, "IX_PTSchedules_PTId");

            entity.HasIndex(e => new { e.Ptid, e.StartTime, e.EndTime }, "IX_PTSchedules_PT_Time");

            entity.Property(e => e.PtscheduleId).HasColumnName("PTScheduleId");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Note).HasMaxLength(500);
            entity.Property(e => e.Ptid).HasColumnName("PTId");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("SCHEDULED");

            entity.HasOne(d => d.Customer).WithMany(p => p.Ptschedules)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PTSchedules_Customers");

            entity.HasOne(d => d.Pt).WithMany(p => p.Ptschedules)
                .HasForeignKey(d => d.Ptid)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PTSchedules_PTs");
        });

        modelBuilder.Entity<Service>(entity =>
        {
            entity.HasKey(e => e.ServiceId).HasName("PK__Services__C51BB00AA26BC064");

            entity.HasIndex(e => e.Name, "UQ_Services_Name").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Name).HasMaxLength(150);
            entity.Property(e => e.Price).HasColumnType("decimal(18, 2)");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__1788CC4CFE643BAE");

            entity.HasIndex(e => e.Email, "UQ_Users_Email").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.PasswordHash).HasMaxLength(500);
            entity.Property(e => e.Role).HasMaxLength(20);
        });

        modelBuilder.Entity<WorkoutExercise>(entity =>
        {
            entity.HasKey(e => e.WorkoutExerciseId).HasName("PK__WorkoutE__2E4E68A911E0DF76");

            entity.HasIndex(e => e.ExerciseId, "IX_WorkoutExercises_ExerciseId");

            entity.HasIndex(e => e.WorkoutPlanId, "IX_WorkoutExercises_WorkoutPlanId");

            entity.HasIndex(e => new { e.WorkoutPlanId, e.DayNumber, e.OrderNumber }, "UQ_WorkoutExercises_Order").IsUnique();

            entity.Property(e => e.Note).HasMaxLength(500);

            entity.HasOne(d => d.Exercise).WithMany(p => p.WorkoutExercises)
                .HasForeignKey(d => d.ExerciseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_WorkoutExercises_Exercises");

            entity.HasOne(d => d.WorkoutPlan).WithMany(p => p.WorkoutExercises)
                .HasForeignKey(d => d.WorkoutPlanId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_WorkoutExercises_WorkoutPlans");
        });

        modelBuilder.Entity<WorkoutPlan>(entity =>
        {
            entity.HasKey(e => e.WorkoutPlanId).HasName("PK__WorkoutP__8C51607B776376E4");

            entity.HasIndex(e => e.CustomerId, "IX_WorkoutPlans_CustomerId");

            entity.HasIndex(e => e.PredictedPlanType, "IX_WorkoutPlans_PredictedPlanType");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.CreatedByAi)
                .HasDefaultValue(true)
                .HasColumnName("CreatedByAI");
            entity.Property(e => e.ExperienceLevel).HasMaxLength(50);
            entity.Property(e => e.Goal).HasMaxLength(100);
            entity.Property(e => e.ModelName).HasMaxLength(100);
            entity.Property(e => e.ModelVersion).HasMaxLength(50);
            entity.Property(e => e.PredictedPlanType).HasMaxLength(100);
            entity.Property(e => e.Title).HasMaxLength(200);

            entity.HasOne(d => d.Customer).WithMany(p => p.WorkoutPlans)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_WorkoutPlans_Customers");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
