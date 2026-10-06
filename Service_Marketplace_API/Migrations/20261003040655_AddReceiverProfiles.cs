using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Service_Marketplace_API.Migrations
{
    /// <inheritdoc />
    public partial class AddReceiverProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReceiverProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DefaultJobLocation = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    PreferredPaymentMethod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    BioOrNotes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ServiceInterests = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReceiverProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReceiverProfiles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReceiverProfiles_UserId",
                table: "ReceiverProfiles",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReceiverProfiles");
        }
    }
}
