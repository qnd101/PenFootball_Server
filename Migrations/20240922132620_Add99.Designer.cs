﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PenFootball_Server.DB;

#nullable disable

namespace PenFootball_Server.Migrations
{
    [DbContext(typeof(UserDataContext))]
    [Migration("20240922132620_Add99")]
    partial class Add99
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "8.0.8");

            modelBuilder.Entity("PenFootball_Server.DB.RelStatModel", b =>
                {
                    b.Property<int>("ID1")
                        .HasColumnType("INTEGER");

                    b.Property<int>("ID2")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Recent")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Win1")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Win2")
                        .HasColumnType("INTEGER");

                    b.HasKey("ID1", "ID2");

                    b.ToTable("RelStats");
                });

            modelBuilder.Entity("PenFootball_Server.Models.UserModel", b =>
                {
                    b.Property<int>("ID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("JoinDate")
                        .HasColumnType("TEXT");

                    b.Property<int>("Loses")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Loses99")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Password")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int>("Rating")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Role")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Wins")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Wins99")
                        .HasColumnType("INTEGER");

                    b.HasKey("ID");

                    b.ToTable("Users");
                });
#pragma warning restore 612, 618
        }
    }
}
