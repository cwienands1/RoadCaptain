// Copyright (c) 2023 Sander van Vliet
// Licensed under Artistic License 2.0
// See LICENSE or https://choosealicense.com/licenses/artistic-2.0/

// <auto-generated />
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using RoadCaptain.App.Web.Adapters.EntityFramework;

#nullable disable

namespace RoadCaptain.App.Web.Migrations
{
    [DbContext(typeof(RoadCaptainDataContext))]
    [Migration("20231210130249_InitialSchema")]
    partial class InitialSchema
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "7.0.3");

            modelBuilder.Entity("RoadCaptain.App.Web.Adapters.EntityFramework.Route", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<decimal>("Ascent")
                        .HasColumnType("TEXT");

                    b.Property<decimal>("Descent")
                        .HasColumnType("TEXT");

                    b.Property<decimal>("Distance")
                        .HasColumnType("TEXT");

                    b.Property<bool>("IsLoop")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .HasColumnType("TEXT");

                    b.Property<string>("Serialized")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<long>("UserId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("World")
                        .HasColumnType("TEXT");

                    b.Property<string>("ZwiftRouteName")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("Routes");
                });

            modelBuilder.Entity("RoadCaptain.App.Web.Adapters.EntityFramework.User", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("ZwiftProfileId")
                        .HasColumnType("TEXT");

                    b.Property<string>("ZwiftSubject")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("RoadCaptain.App.Web.Adapters.EntityFramework.Route", b =>
                {
                    b.HasOne("RoadCaptain.App.Web.Adapters.EntityFramework.User", "User")
                        .WithMany("Routes")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("RoadCaptain.App.Web.Adapters.EntityFramework.User", b =>
                {
                    b.Navigation("Routes");
                });
#pragma warning restore 612, 618
        }
    }
}

