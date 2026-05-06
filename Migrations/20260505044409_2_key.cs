using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PzemReader.Migrations
{
    /// <inheritdoc />
    public partial class _2_key : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EnergyDays",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Day = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EnergyKwh = table.Column<float>(type: "real", nullable: false),
                    MaxPower = table.Column<float>(type: "real", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnergyDays", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EnergyHours",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Hour = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EnergyKwh = table.Column<float>(type: "real", nullable: false),
                    MaxPower = table.Column<float>(type: "real", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnergyHours", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EnergyMinutes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Minute = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EnergyKwh = table.Column<float>(type: "real", nullable: false),
                    MaxPower = table.Column<float>(type: "real", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnergyMinutes", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EnergyDays");

            migrationBuilder.DropTable(
                name: "EnergyHours");

            migrationBuilder.DropTable(
                name: "EnergyMinutes");
        }
    }
}
