using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CameraFeed.Processor.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "resolution",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Width = table.Column<int>(type: "int", nullable: false),
                    Height = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_resolution", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "worker",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CameraId = table.Column<int>(type: "int", nullable: false),
                    Enabled = table.Column<bool>(type: "bit", nullable: false),
                    Framerate = table.Column<int>(type: "int", nullable: false),
                    UseMotiondetection = table.Column<bool>(type: "bit", nullable: false),
                    DownscaleRatio = table.Column<int>(type: "int", nullable: false),
                    MotionRatio = table.Column<double>(type: "float", nullable: false),
                    ResolutionId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_worker", x => x.Id);
                    table.ForeignKey(
                        name: "FK_worker_resolution_ResolutionId",
                        column: x => x.ResolutionId,
                        principalTable: "resolution",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_resolution_Width_Height",
                table: "resolution",
                columns: new[] { "Width", "Height" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_worker_CameraId",
                table: "worker",
                column: "CameraId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_worker_ResolutionId",
                table: "worker",
                column: "ResolutionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "worker");

            migrationBuilder.DropTable(
                name: "resolution");
        }
    }
}
