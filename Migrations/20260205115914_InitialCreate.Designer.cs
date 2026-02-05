using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using UserManagementApp.Data;

#nullable disable

namespace UserManagementApp.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260205115914_InitialCreate")]
    partial class InitialCreate
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.4")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("UserManagementApp.Models.User", b =>
            {
                b.Property<Guid>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("uuid");

                b.Property<string>("Email")
                    .IsRequired()
                    .HasColumnType("text");

                b.Property<string>("EmailConfirmationToken")
                    .HasColumnType("text");

                b.Property<DateTime?>("LastLoginAt")
                    .HasColumnType("timestamp with time zone");

                b.Property<string>("Name")
                    .HasColumnType("text");

                b.Property<string>("PasswordHash")
                    .IsRequired()
                    .HasColumnType("text");

                b.Property<DateTime>("RegisteredAt")
                    .HasColumnType("timestamp with time zone");

                b.Property<int>("Status")
                    .HasColumnType("integer");

                b.HasKey("Id");

                b.HasIndex("Email")
                    .IsUnique()
                    .HasDatabaseName("ux_users_email");

                b.ToTable("Users");
            });
#pragma warning restore 612, 618
        }
    }
}
