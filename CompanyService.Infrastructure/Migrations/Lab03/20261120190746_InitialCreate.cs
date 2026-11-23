using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CompanyService.Infrastructure.Migrations.Lab03
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "companies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_companies", x => x.id);
                    table.CheckConstraint("ck_company_name", "length(trim(name)) > 0");
                });

            migrationBuilder.CreateTable(
                name: "tags",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tags", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "company_details",
                columns: table => new
                {
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    address = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    website = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    founded_year = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_company_details", x => x.company_id);
                    table.CheckConstraint("ck_founded_year", "founded_year BETWEEN 1800 AND 2100");
                    table.ForeignKey(
                        name: "FK_company_details_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "departments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    budget = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_departments", x => x.id);
                    table.CheckConstraint("ck_budget", "budget > 0");
                    table.ForeignKey(
                        name: "FK_departments_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "company_tags",
                columns: table => new
                {
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tag_id = table.Column<Guid>(type: "uuid", nullable: false),
                    added_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_company_tags", x => new { x.company_id, x.tag_id });
                    table.ForeignKey(
                        name: "FK_company_tags_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_company_tags_tags_tag_id",
                        column: x => x.tag_id,
                        principalTable: "tags",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "tags",
                columns: new[] { "id", "name" },
                values: new object[,]
                {
                    { new Guid("50000000-0000-0000-0000-000000000001"), "IT" },
                    { new Guid("50000000-0000-0000-0000-000000000002"), "Remote" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_companies_email",
                table: "companies",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_companies_name",
                table: "companies",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "IX_company_tags_tag_id_company_id",
                table: "company_tags",
                columns: new[] { "tag_id", "company_id" });

            migrationBuilder.CreateIndex(
                name: "IX_departments_company_id_name",
                table: "departments",
                columns: new[] { "company_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tags_name",
                table: "tags",
                column: "name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "company_details");

            migrationBuilder.DropTable(
                name: "company_tags");

            migrationBuilder.DropTable(
                name: "departments");

            migrationBuilder.DropTable(
                name: "tags");

            migrationBuilder.DropTable(
                name: "companies");
        }
    }
}
