using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class AddZ14PersonalCacheSqlite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "stalker_personal_caches",
                columns: table => new
                {
                    cache_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    user_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    map_key = table.Column<string>(type: "TEXT", nullable: false),
                    x = table.Column<float>(type: "REAL", nullable: false),
                    y = table.Column<float>(type: "REAL", nullable: false),
                    z = table.Column<float>(type: "REAL", nullable: false),
                    hidden = table.Column<bool>(type: "INTEGER", nullable: false),
                    current_weight = table.Column<float>(type: "REAL", nullable: false),
                    contents_json = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stalker_personal_caches", x => x.cache_id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_stalker_personal_caches_user_id",
                table: "stalker_personal_caches",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "stalker_personal_caches");
        }
    }
}
