using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Service_Marketplace_API.Migrations
{
    /// <inheritdoc />
    public partial class AddReceiverJobs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReceiverJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReceiverProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceVariantId = table.Column<int>(type: "int", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Budget = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    PaymentMethod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    UrgencyOrPreferredDate = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ExpectedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReceiverJobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReceiverJobs_ReceiverProfiles_ReceiverProfileId",
                        column: x => x.ReceiverProfileId,
                        principalTable: "ReceiverProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReceiverJobs_ServiceVariants_ServiceVariantId",
                        column: x => x.ServiceVariantId,
                        principalTable: "ServiceVariants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ReceiverJobAreas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReceiverJobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CityName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Latitude = table.Column<double>(type: "float", nullable: true),
                    Longitude = table.Column<double>(type: "float", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReceiverJobAreas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReceiverJobAreas_ReceiverJobs_ReceiverJobId",
                        column: x => x.ReceiverJobId,
                        principalTable: "ReceiverJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReceiverJobTags",
                columns: table => new
                {
                    ReceiverJobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TagId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReceiverJobTags", x => new { x.ReceiverJobId, x.TagId });
                    table.ForeignKey(
                        name: "FK_ReceiverJobTags_ReceiverJobs_ReceiverJobId",
                        column: x => x.ReceiverJobId,
                        principalTable: "ReceiverJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReceiverJobTags_Tags_TagId",
                        column: x => x.TagId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReceiverJobAreas_ReceiverJobId",
                table: "ReceiverJobAreas",
                column: "ReceiverJobId");

            migrationBuilder.CreateIndex(
                name: "IX_ReceiverJobs_ReceiverProfileId",
                table: "ReceiverJobs",
                column: "ReceiverProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_ReceiverJobs_ServiceVariantId",
                table: "ReceiverJobs",
                column: "ServiceVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_ReceiverJobTags_TagId",
                table: "ReceiverJobTags",
                column: "TagId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReceiverJobAreas");

            migrationBuilder.DropTable(
                name: "ReceiverJobTags");

            migrationBuilder.DropTable(
                name: "ReceiverJobs");
        }
    }
}
