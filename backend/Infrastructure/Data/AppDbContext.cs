// EF Core 数据库上下文：注册 DbSet，并将实体映射到 Oracle 表（含会籍卡相关表）。
using System;
using System.Collections.Generic;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public partial class AppDbContext : DbContext
{
    public AppDbContext()
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AppUser> AppUsers { get; set; }

    public virtual DbSet<Capacitylog> Capacitylogs { get; set; }

    public virtual DbSet<Checkinout> Checkinouts { get; set; }

    public virtual DbSet<Coach> Coaches { get; set; }

    public virtual DbSet<CoachSchedule> CoachSchedules { get; set; }

    public virtual DbSet<CountCardExtension> CountCardExtensions { get; set; }

    public virtual DbSet<CourseType> CourseTypes { get; set; }

    public virtual DbSet<Employee> Employees { get; set; }

    public virtual DbSet<Equipment> Equipment { get; set; }

    public virtual DbSet<GroupCourseBooking> GroupCourseBookings { get; set; }

    public virtual DbSet<Groupcourse> Groupcourses { get; set; }

    public virtual DbSet<Inspectiontask> Inspectiontasks { get; set; }

    public virtual DbSet<Member> Members { get; set; }

    public virtual DbSet<MemberBenefitCard> MemberBenefitCards { get; set; }

    public virtual DbSet<MemberSchedule> MemberSchedules { get; set; }

    public virtual DbSet<PaymentDetail> PaymentDetails { get; set; }

    public virtual DbSet<PaymentOrder> PaymentOrders { get; set; }

    public virtual DbSet<PersonalCourse> PersonalCourses { get; set; }

    public virtual DbSet<Personalpackage> Personalpackages { get; set; }

    public virtual DbSet<PriceList> PriceLists { get; set; }

    public virtual DbSet<Ptbooking> Ptbookings { get; set; }

    public virtual DbSet<Repairrecord> Repairrecords { get; set; }

    public virtual DbSet<TimeCardExtension> TimeCardExtensions { get; set; }

    public virtual DbSet<TimeSlotInstance> TimeSlotInstances { get; set; }

    public virtual DbSet<TimeSlotTemplate> TimeSlotTemplates { get; set; }

    public virtual DbSet<Venue> Venues { get; set; }

    public virtual DbSet<Voucher> Vouchers { get; set; }



    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .UseCollation("USING_NLS_COMP");

        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.HasKey(e => e.UserId);

            entity.ToTable("USERS");

            entity.HasIndex(e => e.LoginName).IsUnique();

            entity.Property(e => e.UserId)
                .HasPrecision(10)
                .ValueGeneratedNever()
                .HasColumnName("USER_ID");
            entity.Property(e => e.LoginName)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("LOGIN_NAME");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("PASSWORD_HASH");
            entity.Property(e => e.DisplayName)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("DISPLAY_NAME");
            entity.Property(e => e.AvatarUrl)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("AVATAR_URL");
            entity.Property(e => e.Status)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("STATUS");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("DATE")
                .HasColumnName("CREATED_AT");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("DATE")
                .HasColumnName("UPDATED_AT");
        });

        modelBuilder.Entity<Capacitylog>(entity =>
        {
            entity.HasKey(e => e.CapacityLogId).HasName("SYS_C008523");

            entity.ToTable("CAPACITYLOG");

            entity.Property(e => e.CapacityLogId)
                .HasPrecision(10)
                .ValueGeneratedNever()
                .HasColumnName("CAPACITY_LOG_ID");
            entity.Property(e => e.LogTimestamp)
                .HasDefaultValueSql("SYSDATE")
                .HasColumnType("DATE")
                .HasColumnName("LOG_TIMESTAMP");
            entity.Property(e => e.OccupancyRate)
                .HasColumnType("NUMBER(5,2)")
                .HasColumnName("OCCUPANCY_RATE");
            entity.Property(e => e.RecordedCapacity)
                .HasPrecision(10)
                .HasColumnName("RECORDED_CAPACITY");
            entity.Property(e => e.RecordedCount)
                .HasPrecision(10)
                .HasColumnName("RECORDED_COUNT");
            entity.Property(e => e.VenueId)
                .HasPrecision(10)
                .HasColumnName("VENUE_ID");

            entity.HasOne(d => d.Venue).WithMany(p => p.Capacitylogs)
                .HasForeignKey(d => d.VenueId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CAPACITY_VENUE");
        });

        modelBuilder.Entity<Checkinout>(entity =>
        {
            entity.HasKey(e => e.CheckInOutId).HasName("SYS_C008516");

            entity.ToTable("CHECKINOUT");

            entity.Property(e => e.CheckInOutId)
                .HasPrecision(10)
                .ValueGeneratedNever()
                .HasColumnName("CHECK_IN_OUT_ID");
            entity.Property(e => e.CardId)
                .HasPrecision(10)
                .HasColumnName("CARD_ID");
            entity.Property(e => e.CheckInTime)
                .HasDefaultValueSql("SYSDATE ")
                .HasColumnType("DATE")
                .HasColumnName("CHECK_IN_TIME");
            entity.Property(e => e.CheckOutMode)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValueSql("'0' ")
                .IsFixedLength()
                .HasColumnName("CHECK_OUT_MODE");
            entity.Property(e => e.CheckOutTime)
                .HasColumnType("DATE")
                .HasColumnName("CHECK_OUT_TIME");
            entity.Property(e => e.VenueId)
                .HasPrecision(10)
                .HasColumnName("VENUE_ID");

            entity.HasOne(d => d.Card).WithMany(p => p.Checkinouts)
                .HasForeignKey(d => d.CardId)
                .HasConstraintName("FK_CHECK_CARD");

            entity.HasOne(d => d.Venue).WithMany(p => p.Checkinouts)
                .HasForeignKey(d => d.VenueId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CHECK_VENUE");
        });

        modelBuilder.Entity<Coach>(entity =>
        {
            entity.HasKey(e => e.CoachId).HasName("SYS_C008343");

            entity.ToTable("COACH");

            entity.Property(e => e.CoachId)
                .HasPrecision(10)
                .ValueGeneratedNever()
                .HasColumnName("COACH_ID");
            entity.Property(e => e.CoachName)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("COACH_NAME");
            entity.Property(e => e.CoachSummary)
                .HasColumnType("CLOB")
                .HasColumnName("COACH_SUMMARY");
            entity.Property(e => e.HireDate)
                .HasColumnType("DATE")
                .HasColumnName("HIRE_DATE");
            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("PHONE_NUMBER");
            entity.Property(e => e.Sex)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("SEX");
            entity.Property(e => e.Specialty)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("SPECIALTY");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValueSql("'在职' ")
                .HasColumnName("STATUS");
            entity.Property(e => e.UserId)
                .HasPrecision(10)
                .HasColumnName("USER_ID");
        });

        modelBuilder.Entity<CoachSchedule>(entity =>
        {
            entity.HasKey(e => e.ScheduleId).HasName("SYS_C008640");

            entity.ToTable("COACH_SCHEDULE");

            entity.Property(e => e.ScheduleId)
                .HasPrecision(10)
                .ValueGeneratedNever()
                .HasColumnName("SCHEDULE_ID");
            entity.Property(e => e.CoachId)
                .HasPrecision(10)
                .HasColumnName("COACH_ID");
            entity.Property(e => e.ScheduleDate)
                .HasColumnType("DATE")
                .HasColumnName("SCHEDULE_DATE");
            entity.Property(e => e.ScheduleEnd)
                .HasColumnType("DATE")
                .HasColumnName("SCHEDULE_END");
            entity.Property(e => e.ScheduleStart)
                .HasColumnType("DATE")
                .HasColumnName("SCHEDULE_START");
            entity.Property(e => e.ScheduleType)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("SCHEDULE_TYPE");
            entity.Property(e => e.SourceRecordId)
                .HasPrecision(10)
                .HasColumnName("SOURCE_RECORD_ID");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValueSql("'正常'\r\n                       ")
                .HasColumnName("STATUS");

            entity.HasOne(d => d.Coach).WithMany(p => p.CoachSchedules)
                .HasForeignKey(d => d.CoachId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SCHEDULE_COACH");
        });

        modelBuilder.Entity<CountCardExtension>(entity =>
        {
            entity.HasKey(e => e.CardId).HasName("SYS_C008616");

            entity.ToTable("COUNT_CARD_EXTENSION");

            entity.Property(e => e.CardId)
                .HasPrecision(10)
                .ValueGeneratedNever()
                .HasColumnName("CARD_ID");
            entity.Property(e => e.RemainingCount)
                .HasPrecision(6)
                .HasColumnName("REMAINING_COUNT");
            entity.Property(e => e.TotalCounts)
                .HasPrecision(6)
                .HasColumnName("TOTAL_COUNTS");

            entity.HasOne(d => d.Card).WithOne(p => p.CountCardExtension)
                .HasForeignKey<CountCardExtension>(d => d.CardId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_COUNT_CARD");
        });

        modelBuilder.Entity<CourseType>(entity =>
        {
            entity.HasKey(e => e.TypeId).HasName("SYS_C008339");

            entity.ToTable("COURSE_TYPE");

            entity.Property(e => e.TypeId)
                .HasPrecision(10)
                .ValueGeneratedNever()
                .HasColumnName("TYPE_ID");
            entity.Property(e => e.TypeName)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("TYPE_NAME");
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasKey(e => e.EmpId).HasName("SYS_C008425");

            entity.ToTable("EMPLOYEE", tb => tb.HasComment("员工信息"));

            entity.Property(e => e.EmpId)
                .HasPrecision(10)
                .ValueGeneratedNever()
                .HasColumnName("EMP_ID");
            entity.Property(e => e.EmpName)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("EMP_NAME");
            entity.Property(e => e.Phone)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("PHONE");
            entity.Property(e => e.Role)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("ROLE");
            entity.Property(e => e.Status)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValueSql("'0'\r\n")
                .IsFixedLength()
                .HasComment("1-在职，0-离职")
                .HasColumnName("STATUS");
            entity.Property(e => e.UserId)
                .HasPrecision(10)
                .HasColumnName("USER_ID");
        });

        modelBuilder.Entity<Equipment>(entity =>
        {
            entity.HasKey(e => e.EquipId).HasName("SYS_C008427");

            entity.ToTable("EQUIPMENT", tb => tb.HasComment("健身器材"));

            entity.Property(e => e.EquipId)
                .HasPrecision(10)
                .ValueGeneratedNever()
                .HasColumnName("EQUIP_ID");
            entity.Property(e => e.EquipName)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("EQUIP_NAME");
            entity.Property(e => e.PurchaseDate)
                .HasColumnType("DATE")
                .HasColumnName("PURCHASE_DATE");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValueSql("'正常'")
                .HasColumnName("STATUS");
            entity.Property(e => e.VenueId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("VENUE_ID");
        });

        modelBuilder.Entity<GroupCourseBooking>(entity =>
        {
            entity.HasKey(e => e.BookingId).HasName("SYS_C008630");

            entity.ToTable("GROUP_COURSE_BOOKING");

            entity.HasIndex(e => new { e.MemberId, e.CourseId }, "UK_MEMBER_COURSE").IsUnique();

            entity.Property(e => e.BookingId)
                .HasPrecision(10)
                .ValueGeneratedNever()
                .HasColumnName("BOOKING_ID");
            entity.Property(e => e.BookingStatus)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValueSql("'0' ")
                .IsFixedLength()
                .HasColumnName("BOOKING_STATUS");
            entity.Property(e => e.BookingTime)
                .HasDefaultValueSql("SYSDATE")
                .HasColumnType("DATE")
                .HasColumnName("BOOKING_TIME");
            entity.Property(e => e.CourseId)
                .HasPrecision(10)
                .HasColumnName("COURSE_ID");
            entity.Property(e => e.MemberId)
                .HasPrecision(10)
                .HasColumnName("MEMBER_ID");

            entity.HasOne(d => d.Course).WithMany(p => p.GroupCourseBookings)
                .HasForeignKey(d => d.CourseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BOOKING_COURSE");

            entity.HasOne(d => d.Member).WithMany(p => p.GroupCourseBookings)
                .HasForeignKey(d => d.MemberId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BOOKING_MEMBER");
        });

        modelBuilder.Entity<Groupcourse>(entity =>
        {
            entity.HasKey(e => e.CourseId).HasName("SYS_C008366");

            entity.ToTable("GROUPCOURSE");

            entity.Property(e => e.CourseId)
                .HasPrecision(10)
                .ValueGeneratedNever()
                .HasColumnName("COURSE_ID");
            entity.Property(e => e.CoachId)
                .HasPrecision(10)
                .HasColumnName("COACH_ID");
            entity.Property(e => e.CourseName)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("COURSE_NAME");
            entity.Property(e => e.CourseSummary)
                .HasColumnType("CLOB")
                .HasColumnName("COURSE_SUMMARY");
            entity.Property(e => e.CurrentCapacity)
                .HasPrecision(5)
                .HasDefaultValueSql("0 ")
                .HasColumnName("CURRENT_CAPACITY");
            entity.Property(e => e.MaxCapacity)
                .HasPrecision(5)
                .HasColumnName("MAX_CAPACITY");
            entity.Property(e => e.TimeSlotId)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("TIME_SLOT_ID");
            entity.Property(e => e.TypeId)
                .HasPrecision(10)
                .HasColumnName("TYPE_ID");

            entity.HasOne(d => d.Coach).WithMany(p => p.Groupcourses)
                .HasForeignKey(d => d.CoachId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_COURSE_COACH");

            entity.HasOne(d => d.TimeSlot).WithMany(p => p.Groupcourses)
                .HasForeignKey(d => d.TimeSlotId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_COURSE_TIMESLOT");

            entity.HasOne(d => d.Type).WithMany(p => p.Groupcourses)
                .HasForeignKey(d => d.TypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_COURSE_TYPE");
        });

        modelBuilder.Entity<Inspectiontask>(entity =>
        {
            entity.HasKey(e => e.TaskId).HasName("SYS_C008435");

            entity.ToTable("INSPECTIONTASK", tb => tb.HasComment("卫生巡检任务"));

            entity.Property(e => e.TaskId)
                .HasPrecision(10)
                .ValueGeneratedNever()
                .HasColumnName("TASK_ID");
            entity.Property(e => e.EmpId)
                .HasPrecision(10)
                .HasColumnName("EMP_ID");
            entity.Property(e => e.Remark)
                .HasMaxLength(200)
                .IsUnicode(false)
                .HasColumnName("REMARK");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValueSql("'待执行'")
                .HasColumnName("STATUS");
            entity.Property(e => e.TaskTime)
                .HasColumnType("DATE")
                .HasColumnName("TASK_TIME");
            entity.Property(e => e.VenueId)
                .HasPrecision(10)
                .HasColumnName("VENUE_ID");

            entity.HasOne(d => d.Emp).WithMany(p => p.Inspectiontasks)
                .HasForeignKey(d => d.EmpId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_INSPECT_EMP");
        });

        modelBuilder.Entity<Member>(entity =>
        {
            entity.HasKey(e => e.MemberId).HasName("SYS_C008487");

            entity.ToTable("MEMBER");

            entity.HasIndex(e => e.PhoneNumber, "SYS_C008488").IsUnique();

            entity.HasIndex(e => e.IdCard, "SYS_C008489").IsUnique();

            entity.Property(e => e.MemberId)
                .HasPrecision(10)
                .ValueGeneratedNever()
                .HasColumnName("MEMBER_ID");
            entity.Property(e => e.Birthday)
                .HasColumnType("DATE")
                .HasColumnName("BIRTHDAY");
            entity.Property(e => e.Gender)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("GENDER");
            entity.Property(e => e.IdCard)
                .HasMaxLength(18)
                .IsUnicode(false)
                .HasColumnName("ID_CARD");
            entity.Property(e => e.MemberLevel)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("MEMBER_LEVEL");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("NAME");
            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("PHONE_NUMBER");
            entity.Property(e => e.RegisterDate)
                .HasDefaultValueSql("SYSDATE")
                .HasColumnType("DATE")
                .HasColumnName("REGISTER_DATE");
            entity.Property(e => e.Status)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValueSql("'1' ")
                .IsFixedLength()
                .HasColumnName("STATUS");
            entity.Property(e => e.UserId)
                .HasPrecision(10)
                .HasColumnName("USER_ID");

            entity.HasOne<AppUser>()
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .HasConstraintName("FK_MEMBER_USERS");
        });

        modelBuilder.Entity<MemberBenefitCard>(entity =>
        {
            entity.HasKey(e => e.CardId).HasName("SYS_C008500");

            entity.ToTable("MEMBER_BENEFIT_CARD");

            entity.Property(e => e.CardId)
                .HasPrecision(10)
                .ValueGeneratedNever()
                .HasColumnName("CARD_ID");
            entity.Property(e => e.CardStatus)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValueSql("'1'")
                .IsFixedLength()
                .HasColumnName("CARD_STATUS");
            entity.Property(e => e.CardType)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("CARD_TYPE");
            entity.Property(e => e.IssueDate)
                .HasDefaultValueSql("SYSDATE")
                .HasColumnType("DATE")
                .HasColumnName("ISSUE_DATE");
            entity.Property(e => e.MemberId)
                .HasPrecision(10)
                .HasColumnName("MEMBER_ID");

            entity.HasOne(d => d.Member).WithMany(p => p.MemberBenefitCards)
                .HasForeignKey(d => d.MemberId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CARD_MEMBER");
        });

        modelBuilder.Entity<MemberSchedule>(entity =>
        {
            entity.HasKey(e => e.ScheduleId).HasName("SYS_C008625");

            entity.ToTable("MEMBER_SCHEDULE");

            entity.Property(e => e.ScheduleId)
                .HasPrecision(10)
                .ValueGeneratedNever()
                .HasColumnName("SCHEDULE_ID");
            entity.Property(e => e.MemberId)
                .HasPrecision(10)
                .HasColumnName("MEMBER_ID");
            entity.Property(e => e.ScheduleDate)
                .HasColumnType("DATE")
                .HasColumnName("SCHEDULE_DATE");
            entity.Property(e => e.ScheduleEnd)
                .HasColumnType("DATE")
                .HasColumnName("SCHEDULE_END");
            entity.Property(e => e.ScheduleStart)
                .HasColumnType("DATE")
                .HasColumnName("SCHEDULE_START");
            entity.Property(e => e.ScheduleType)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("SCHEDULE_TYPE");
            entity.Property(e => e.SourceRecordId)
                .HasPrecision(10)
                .HasColumnName("SOURCE_RECORD_ID");
            entity.Property(e => e.Status)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValueSql("'1' ")
                .IsFixedLength()
                .HasColumnName("STATUS");

            entity.HasOne(d => d.Member).WithMany(p => p.MemberSchedules)
                .HasForeignKey(d => d.MemberId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SCHEDULE_MEMBER");
        });

        modelBuilder.Entity<PaymentDetail>(entity =>
        {
            entity.HasKey(e => e.DetailId).HasName("SYS_C008660");

            entity.ToTable("PAYMENT_DETAIL");

            entity.Property(e => e.DetailId)
                .HasPrecision(10)
                .ValueGeneratedNever()
                .HasColumnName("DETAIL_ID");
            entity.Property(e => e.OrderId)
                .HasPrecision(10)
                .HasColumnName("ORDER_ID");
            entity.Property(e => e.PriceId)
                .HasPrecision(10)
                .HasColumnName("PRICE_ID");
            entity.Property(e => e.Quantity)
                .HasPrecision(10)
                .HasDefaultValueSql("1 ")
                .HasColumnName("QUANTITY");
            entity.Property(e => e.SubtotalAmount)
                .HasColumnType("NUMBER(10,2)")
                .HasColumnName("SUBTOTAL_AMOUNT");
            entity.Property(e => e.TransactionPrice)
                .HasColumnType("NUMBER(10,2)")
                .HasColumnName("TRANSACTION_PRICE");

            entity.HasOne(d => d.Order).WithMany(p => p.PaymentDetails)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DETAIL_ORDER");

            entity.HasOne(d => d.Price).WithMany(p => p.PaymentDetails)
                .HasForeignKey(d => d.PriceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DETAIL_PRICE");
        });

        modelBuilder.Entity<PaymentOrder>(entity =>
        {
            entity.HasKey(e => e.OrderId).HasName("SYS_C008650");

            entity.ToTable("PAYMENT_ORDER");

            entity.Property(e => e.OrderId)
                .HasPrecision(10)
                .ValueGeneratedNever()
                .HasColumnName("ORDER_ID");
            entity.Property(e => e.BusinessOrderId)
                .HasPrecision(10)
                .HasColumnName("BUSINESS_ORDER_ID");
            entity.Property(e => e.CreateTime)
                .HasDefaultValueSql("SYSDATE")
                .HasColumnType("DATE")
                .HasColumnName("CREATE_TIME");
            entity.Property(e => e.PaymentFinishTime)
                .HasColumnType("DATE")
                .HasColumnName("PAYMENT_FINISH_TIME");
            entity.Property(e => e.PaymentStatus)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("PAYMENT_STATUS");
            entity.Property(e => e.TotalAmount)
                .HasColumnType("NUMBER(10,2)")
                .HasColumnName("TOTAL_AMOUNT");
            entity.Property(e => e.VoucherId)
                .HasPrecision(10)
                .HasColumnName("VOUCHER_ID");

            entity.HasOne(d => d.Voucher).WithMany(p => p.PaymentOrders)
                .HasForeignKey(d => d.VoucherId)
                .HasConstraintName("FK_ORDER_VOUCHER");
        });

        modelBuilder.Entity<PersonalCourse>(entity =>
        {
            entity.HasKey(e => e.PersonalCourseId).HasName("SYS_C008643");

            entity.ToTable("PERSONAL_COURSE");

            entity.Property(e => e.PersonalCourseId)
                .HasPrecision(10)
                .ValueGeneratedNever()
                .HasColumnName("PERSONAL_COURSE_ID");
            entity.Property(e => e.CoachId)
                .HasPrecision(10)
                .HasColumnName("COACH_ID");
            entity.Property(e => e.CourseDescription)
                .HasColumnType("CLOB")
                .HasColumnName("COURSE_DESCRIPTION");
            entity.Property(e => e.CourseName)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("COURSE_NAME");

            entity.HasOne(d => d.Coach).WithMany(p => p.PersonalCourses)
                .HasForeignKey(d => d.CoachId)
                .HasConstraintName("FK_PERSONAL_COURSE_COACH");
        });

        modelBuilder.Entity<Personalpackage>(entity =>
        {
            entity.HasKey(e => e.PackageId);

            entity.ToTable("PERSONALPACKAGE");

            entity.Property(e => e.PackageId)
                .HasPrecision(10)
                .ValueGeneratedNever()
                .HasColumnName("PACKAGE_ID");
            entity.Property(e => e.CoachId)
                .HasPrecision(10)
                .HasColumnName("COACH_ID");
            entity.Property(e => e.ExpireDate)
                .HasColumnType("DATE")
                .HasColumnName("EXPIRE_DATE");
            entity.Property(e => e.MemberId)
                .HasPrecision(10)
                .HasColumnName("MEMBER_ID");
            entity.Property(e => e.PackageStatus)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("PACKAGE_STATUS");
            entity.Property(e => e.PersonalCourseId)
                .HasPrecision(10)
                .HasColumnName("PERSONAL_COURSE_ID");
            entity.Property(e => e.RemainingSessions)
                .HasPrecision(5)
                .HasColumnName("REMAINING_SESSIONS");
            entity.Property(e => e.TotalSessions)
                .HasPrecision(5)
                .HasColumnName("TOTAL_SESSIONS");

            entity.HasOne(d => d.Coach).WithMany(p => p.Personalpackages)
                .HasForeignKey(d => d.CoachId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PP_COACH");

            entity.HasOne(d => d.PersonalCourse).WithMany(p => p.Personalpackages)
                .HasForeignKey(d => d.PersonalCourseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PACKAGE_PERSONAL_COURSE");
        });

        modelBuilder.Entity<PriceList>(entity =>
        {
            entity.HasKey(e => e.PriceId).HasName("SYS_C008654");

            entity.ToTable("PRICE_LIST");

            entity.Property(e => e.PriceId)
                .HasPrecision(10)
                .ValueGeneratedNever()
                .HasColumnName("PRICE_ID");
            entity.Property(e => e.PriceUpdateTime)
                .HasDefaultValueSql("SYSDATE\r\n")
                .HasColumnType("DATE")
                .HasColumnName("PRICE_UPDATE_TIME");
            entity.Property(e => e.ProductType)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("PRODUCT_TYPE");
            entity.Property(e => e.StandardPrice)
                .HasColumnType("NUMBER(10,2)")
                .HasColumnName("STANDARD_PRICE");
        });

        modelBuilder.Entity<Ptbooking>(entity =>
        {
            entity.ToTable("PTBOOKING");

            entity.Property(e => e.PtBookingId)
                .HasPrecision(10)
                .ValueGeneratedNever()
                .HasColumnName("PT_BOOKING_ID");
            entity.Property(e => e.BookingTime)
                .HasDefaultValueSql("SYSDATE ")
                .HasColumnType("DATE")
                .HasColumnName("BOOKING_TIME");
            entity.Property(e => e.CoachConfirmed)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValueSql("'0' ")
                .IsFixedLength()
                .HasColumnName("COACH_CONFIRMED");
            entity.Property(e => e.CoachId)
                .HasPrecision(10)
                .HasColumnName("COACH_ID");
            entity.Property(e => e.MemberConfirmed)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValueSql("'0' ")
                .IsFixedLength()
                .HasColumnName("MEMBER_CONFIRMED");
            entity.Property(e => e.MemberId)
                .HasPrecision(10)
                .HasColumnName("MEMBER_ID");
            entity.Property(e => e.PackageId)
                .HasPrecision(10)
                .HasColumnName("PACKAGE_ID");
            entity.Property(e => e.SessionTime)
                .HasColumnType("DATE")
                .HasColumnName("SESSION_TIME");

            entity.HasOne(d => d.Coach).WithMany(p => p.Ptbookings)
                .HasForeignKey(d => d.CoachId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PB_COACH");

            entity.HasOne(d => d.Package).WithMany(p => p.Ptbookings)
                .HasForeignKey(d => d.PackageId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PB_PACKAGE");
        });

        modelBuilder.Entity<Repairrecord>(entity =>
        {
            entity.HasKey(e => e.RecordId).HasName("SYS_C008429");

            entity.ToTable("REPAIRRECORD", tb => tb.HasComment("器材维修记录"));

            entity.Property(e => e.RecordId)
                .HasPrecision(10)
                .ValueGeneratedNever()
                .HasColumnName("RECORD_ID");
            entity.Property(e => e.Description)
                .HasMaxLength(200)
                .IsUnicode(false)
                .HasColumnName("DESCRIPTION");
            entity.Property(e => e.EmpId)
                .HasPrecision(10)
                .HasColumnName("EMP_ID");
            entity.Property(e => e.EquipId)
                .HasPrecision(10)
                .HasColumnName("EQUIP_ID");
            entity.Property(e => e.RepairCost)
                .HasDefaultValueSql("0")
                .HasColumnType("NUMBER(20,2)")
                .HasColumnName("REPAIR_COST");
            entity.Property(e => e.ReportTime)
                .HasDefaultValueSql("SYSDATE")
                .HasColumnType("DATE")
                .HasColumnName("REPORT_TIME");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValueSql("'待处理'")
                .HasColumnName("STATUS");

            entity.HasOne(d => d.Emp).WithMany(p => p.Repairrecords)
                .HasForeignKey(d => d.EmpId)
                .HasConstraintName("FK_REPAIR_EMP");

            entity.HasOne(d => d.Equip).WithMany(p => p.Repairrecords)
                .HasForeignKey(d => d.EquipId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_REPAIR_EQUIP");
        });

        modelBuilder.Entity<TimeCardExtension>(entity =>
        {
            entity.HasKey(e => e.CardId).HasName("SYS_C008609");

            entity.ToTable("TIME_CARD_EXTENSION");

            entity.Property(e => e.CardId)
                .HasPrecision(10)
                .ValueGeneratedNever()
                .HasColumnName("CARD_ID");
            entity.Property(e => e.ExpireDate)
                .HasColumnType("DATE")
                .HasColumnName("EXPIRE_DATE");

            entity.HasOne(d => d.Card).WithOne(p => p.TimeCardExtension)
                .HasForeignKey<TimeCardExtension>(d => d.CardId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_TIME_CARD");
        });

        modelBuilder.Entity<TimeSlotInstance>(entity =>
        {
            entity.HasKey(e => new { e.TimeSlotId, e.StartTime, e.CourseDate }).HasName("SYS_C008356");

            entity.ToTable("TIME_SLOT_INSTANCE");

            entity.Property(e => e.TimeSlotId)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("TIME_SLOT_ID");
            entity.Property(e => e.StartTime)
                .HasPrecision(6)
                .HasColumnName("START_TIME");
            entity.Property(e => e.CourseDate)
                .HasColumnType("DATE")
                .HasColumnName("COURSE_DATE");
            entity.Property(e => e.EndTime)
                .HasPrecision(6)
                .HasColumnName("END_TIME");

            entity.HasOne(d => d.TimeSlot).WithMany(p => p.TimeSlotInstances)
                .HasForeignKey(d => d.TimeSlotId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TIMESLOT_TEMPLATE");
        });

        modelBuilder.Entity<TimeSlotTemplate>(entity =>
        {
            entity.HasKey(e => e.TimeSlotId).HasName("SYS_C008350");

            entity.ToTable("TIME_SLOT_TEMPLATE");

            entity.Property(e => e.TimeSlotId)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("TIME_SLOT_ID");
        });

        modelBuilder.Entity<Venue>(entity =>
        {
            entity.HasKey(e => e.VenueId).HasName("SYS_C008495");

            entity.ToTable("VENUE");

            entity.Property(e => e.VenueId)
                .HasPrecision(10)
                .ValueGeneratedNever()
                .HasColumnName("VENUE_ID");
            entity.Property(e => e.CurrentCapacity)
                .HasPrecision(5)
                .HasDefaultValueSql("0 ")
                .HasColumnName("CURRENT_CAPACITY");
            entity.Property(e => e.MaxCapacity)
                .HasPrecision(5)
                .HasColumnName("MAX_CAPACITY");
            entity.Property(e => e.VenueName)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("VENUE_NAME");
            entity.Property(e => e.VenueStatus)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValueSql("'1' ")
                .IsFixedLength()
                .HasColumnName("VENUE_STATUS");
        });

        modelBuilder.Entity<Voucher>(entity =>
        {
            entity.HasKey(e => e.VoucherId).HasName("SYS_C008441");

            entity.ToTable("VOUCHER", tb => tb.HasComment("会员福利券记录表"));

            entity.Property(e => e.VoucherId)
                .HasPrecision(10)
                .ValueGeneratedNever()
                .HasColumnName("VOUCHER_ID");
            entity.Property(e => e.DiscountValue)
                .HasColumnType("NUMBER(10,2)")
                .HasColumnName("DISCOUNT_VALUE");
            entity.Property(e => e.MemberId)
                .HasPrecision(10)
                .HasColumnName("MEMBER_ID");
            entity.Property(e => e.Status)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValueSql("'0'\r\n")
                .IsFixedLength()
                .HasComment("0-未使用，1-已核销，2-已过期")
                .HasColumnName("STATUS");
            entity.Property(e => e.ValidUntil)
                .HasColumnType("DATE")
                .HasColumnName("VALID_UNTIL");
            entity.Property(e => e.VoucherType)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("VOUCHER_TYPE");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}


