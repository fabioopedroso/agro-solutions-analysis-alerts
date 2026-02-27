using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class agrosolutionsanalysis : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Alerts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FieldId = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Severity = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Message = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    TriggerValue = table.Column<double>(type: "double precision", precision: 10, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Alerts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SensorReadings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FieldId = table.Column<int>(type: "integer", nullable: false),
                    SensorType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Value = table.Column<double>(type: "double precision", precision: 10, scale: 2, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SensorReadings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_FieldId",
                table: "Alerts",
                column: "FieldId");

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_FieldId_Status",
                table: "Alerts",
                columns: new[] { "FieldId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_Type_Severity",
                table: "Alerts",
                columns: new[] { "Type", "Severity" });

            migrationBuilder.CreateIndex(
                name: "IX_SensorReadings_FieldId",
                table: "SensorReadings",
                column: "FieldId");

            migrationBuilder.CreateIndex(
                name: "IX_SensorReadings_FieldId_SensorType_Timestamp",
                table: "SensorReadings",
                columns: new[] { "FieldId", "SensorType", "Timestamp" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Alerts");

            migrationBuilder.DropTable(
                name: "SensorReadings");
        }
    }
}
