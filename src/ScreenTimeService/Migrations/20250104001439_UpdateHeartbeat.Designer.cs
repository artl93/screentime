﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace ScreenTimeAPI.Migrations
{
    [DbContext(typeof(UserContext))]
    [Migration("20250104001439_UpdateHeartbeat")]
    partial class UpdateHeartbeat
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("ScreenTimeService.DailyConfiguration", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<int>("DailyLimitMinutes")
                        .HasColumnType("int");

                    b.Property<int>("GraceMinutes")
                        .HasColumnType("int");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<int>("WarningIntervalSeconds")
                        .HasColumnType("int");

                    b.Property<int>("WarningTimeMinutes")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.ToTable("DailyConfigurations");
                });

            modelBuilder.Entity("ScreenTimeService.ExtensionRequest", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<TimeSpan>("Duration")
                        .HasColumnType("time");

                    b.Property<Guid?>("ExtensionRequestResponseId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTimeOffset>("SubmissionDate")
                        .HasColumnType("datetimeoffset");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("ExtensionRequestResponseId");

                    b.ToTable("ExtensionRequests");
                });

            modelBuilder.Entity("ScreenTimeService.ExtensionRequestResponse", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTimeOffset>("DateTime")
                        .HasColumnType("datetimeoffset");

                    b.Property<TimeSpan>("Duration")
                        .HasColumnType("time");

                    b.Property<Guid>("ExtensionRequestId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTimeOffset>("ForDate")
                        .HasColumnType("datetimeoffset");

                    b.Property<bool>("IsApproved")
                        .HasColumnType("bit");

                    b.HasKey("Id");

                    b.ToTable("ExtensionRequestResponses");
                });

            modelBuilder.Entity("ScreenTimeService.HeartbeatRecord", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTimeOffset>("DateTime")
                        .HasColumnType("datetimeoffset");

                    b.Property<TimeSpan>("Duration")
                        .HasColumnType("time");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<int>("UserState")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.ToTable("Heartbeats");
                });

            modelBuilder.Entity("ScreenTimeService.ScreenTimeSummary", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTimeOffset>("DateTime")
                        .HasColumnType("datetimeoffset");

                    b.Property<TimeSpan>("Extensions")
                        .HasColumnType("time");

                    b.Property<TimeSpan>("TotalDuration")
                        .HasColumnType("time");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.ToTable("ScreenTimeSummaries");
                });

            modelBuilder.Entity("ScreenTimeService.UserRecord", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("UserName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("ScreenTimeService.WeeklyConfiguration", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("DefaultId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid?>("FridayId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid?>("MondayId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid?>("SaturdayId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid?>("SundayId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid?>("ThursdayId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid?>("TuesdayId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid?>("WednesdayId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("DefaultId");

                    b.HasIndex("FridayId");

                    b.HasIndex("MondayId");

                    b.HasIndex("SaturdayId");

                    b.HasIndex("SundayId");

                    b.HasIndex("ThursdayId");

                    b.HasIndex("TuesdayId");

                    b.HasIndex("WednesdayId");

                    b.ToTable("WeeklyConfigurations");
                });

            modelBuilder.Entity("ScreenTimeService.ExtensionRequest", b =>
                {
                    b.HasOne("ScreenTimeService.ExtensionRequestResponse", null)
                        .WithMany("ExtensionRequests")
                        .HasForeignKey("ExtensionRequestResponseId");
                });

            modelBuilder.Entity("ScreenTimeService.WeeklyConfiguration", b =>
                {
                    b.HasOne("ScreenTimeService.DailyConfiguration", "Default")
                        .WithMany()
                        .HasForeignKey("DefaultId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("ScreenTimeService.DailyConfiguration", "Friday")
                        .WithMany()
                        .HasForeignKey("FridayId");

                    b.HasOne("ScreenTimeService.DailyConfiguration", "Monday")
                        .WithMany()
                        .HasForeignKey("MondayId");

                    b.HasOne("ScreenTimeService.DailyConfiguration", "Saturday")
                        .WithMany()
                        .HasForeignKey("SaturdayId");

                    b.HasOne("ScreenTimeService.DailyConfiguration", "Sunday")
                        .WithMany()
                        .HasForeignKey("SundayId");

                    b.HasOne("ScreenTimeService.DailyConfiguration", "Thursday")
                        .WithMany()
                        .HasForeignKey("ThursdayId");

                    b.HasOne("ScreenTimeService.DailyConfiguration", "Tuesday")
                        .WithMany()
                        .HasForeignKey("TuesdayId");

                    b.HasOne("ScreenTimeService.DailyConfiguration", "Wednesday")
                        .WithMany()
                        .HasForeignKey("WednesdayId");

                    b.Navigation("Default");

                    b.Navigation("Friday");

                    b.Navigation("Monday");

                    b.Navigation("Saturday");

                    b.Navigation("Sunday");

                    b.Navigation("Thursday");

                    b.Navigation("Tuesday");

                    b.Navigation("Wednesday");
                });

            modelBuilder.Entity("ScreenTimeService.ExtensionRequestResponse", b =>
                {
                    b.Navigation("ExtensionRequests");
                });
#pragma warning restore 612, 618
        }
    }
}
