using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace StudentERP.Models;

public partial class StudentErpContext : DbContext
{
    public StudentErpContext()
    {
    }

    public StudentErpContext(DbContextOptions<StudentErpContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AdmissionDetail> AdmissionDetails { get; set; }

    public virtual DbSet<ContactDetail> ContactDetails { get; set; }

    public virtual DbSet<ParentsDetails> ParentsDetails { get; set; }

    public virtual DbSet<PersonalDetails> PersonalDetails { get; set; }

    public virtual DbSet<Student> Students { get; set; }

    public virtual DbSet<StudentBatch> StudentBatches { get; set; }

    public virtual DbSet<StudentLogin> StudentLogins { get; set; }

    public virtual DbSet<StudentProfile> StudentProfiles { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=LAPTOP-1PLP1ANI\\SQLEXPRESS; Database=StudentERP; Integrated Security=True; TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AdmissionDetail>(entity =>
        {
            entity.HasKey(e => e.StudentId).HasName("PK_AdmissionDetail");

            entity.Property(e => e.StudentId).ValueGeneratedNever();
            entity.Property(e => e.AdmissionNumber).HasMaxLength(50);
            entity.Property(e => e.Course).HasMaxLength(255);

            entity.HasOne(d => d.Student).WithOne(p => p.AdmissionDetail)
                .HasForeignKey<AdmissionDetail>(d => d.StudentId)
                .HasConstraintName("FK_AdmissionDetail_StudentLogin_StudentId");
        });

        modelBuilder.Entity<ContactDetail>(entity =>
        {
            entity.HasKey(e => e.StudentId);

            entity.Property(e => e.StudentId).ValueGeneratedNever();
            entity.Property(e => e.Address).HasMaxLength(255);
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.PhoneNumber).HasMaxLength(50);
            entity.Property(e => e.State).HasMaxLength(100);
            entity.Property(e => e.ZipCode).HasMaxLength(20);

            entity.HasOne(d => d.Student).WithOne(p => p.ContactDetail).HasForeignKey<ContactDetail>(d => d.StudentId);
        });

        modelBuilder.Entity<ParentsDetails>(entity =>
        {
            entity.HasKey(e => e.StudentId).HasName("PK_ParentsDetail");

            entity.Property(e => e.StudentId).ValueGeneratedNever();
            entity.Property(e => e.FatherName).HasMaxLength(255);
            entity.Property(e => e.MotherName).HasMaxLength(255);
            entity.Property(e => e.ParentPhoneNumber).HasMaxLength(50);

            entity.HasOne(d => d.Student).WithOne(p => p.ParentsDetail)
                .HasForeignKey<ParentsDetails>(d => d.StudentId)
                .HasConstraintName("FK_ParentsDetail_StudentLogin_StudentId");
        });

        modelBuilder.Entity<PersonalDetails>(entity =>
        {
            entity.HasKey(e => e.StudentId);

            entity.Property(e => e.StudentId).ValueGeneratedNever();
            entity.Property(e => e.DateOfBirth).HasColumnType("datetime");
            entity.Property(e => e.FirstName).HasMaxLength(255);
            entity.Property(e => e.Gender).HasMaxLength(50);
            entity.Property(e => e.LastName).HasMaxLength(255);

            entity.HasOne(d => d.Student).WithOne(p => p.PersonalDetail)
                .HasForeignKey<PersonalDetails>(d => d.StudentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PersonalDetails_Student");
        });

        modelBuilder.Entity<Student>(entity =>
        {
            entity.HasKey(e => e.StudentId).HasName("PK__Students__32C52A79638954A3");

            entity.HasIndex(e => e.Email, "UQ__Students__A9D10534E5E712C6").IsUnique();

            entity.Property(e => e.StudentId).HasColumnName("StudentID");
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.EnrollmentDate).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.FirstName).HasMaxLength(50);
            entity.Property(e => e.LastName).HasMaxLength(50);
        });

        modelBuilder.Entity<StudentBatch>(entity =>
        {
            entity.HasKey(e => e.BatchId).HasName("PK__StudentB__5D55CE38DD2EF535");

            entity.ToTable("StudentBatch");

            entity.Property(e => e.BatchId).HasColumnName("BatchID");
            entity.Property(e => e.Period).HasMaxLength(100);
            entity.Property(e => e.ProfileStatus).HasMaxLength(50);
        });

        modelBuilder.Entity<StudentLogin>(entity =>
        {
            entity.HasKey(e => e.StudentId).HasName("PK__StudentL__32C52A79832EC911");

            entity.ToTable("StudentLogin");

            entity.HasIndex(e => e.StudentMail, "UQ__StudentL__71B0EA105A52C5E4").IsUnique();

            entity.Property(e => e.StudentId)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("StudentID");
            entity.Property(e => e.HashPassword).HasMaxLength(255);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.StudentMail).HasMaxLength(255);
        });

        modelBuilder.Entity<StudentProfile>(entity =>
        {
            entity.HasKey(e => e.StudentId);

            entity.ToTable("StudentProfile");

            entity.Property(e => e.StudentId).ValueGeneratedNever();

            entity.HasOne(d => d.Student).WithOne(p => p.StudentProfile).HasForeignKey<StudentProfile>(d => d.StudentId);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__1788CCACDCADF125");

            entity.HasIndex(e => e.Username, "UQ__Users__536C85E4576FA28B").IsUnique();

            entity.Property(e => e.UserId).HasColumnName("UserID");
            entity.Property(e => e.PasswordHash).HasMaxLength(255);
            entity.Property(e => e.Role).HasMaxLength(20);
            entity.Property(e => e.StudentId).HasColumnName("StudentID");
            entity.Property(e => e.Username).HasMaxLength(50);

            entity.HasOne(d => d.Student).WithMany(p => p.Users)
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_Users_Students");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
