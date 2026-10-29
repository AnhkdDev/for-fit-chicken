using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ForFitChicken.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMediaGovernance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MediaObjectReservations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StorageKey = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ByteSize = table.Column<long>(type: "bigint", nullable: false),
                    State = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ActorUserId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ActiveUntilUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaObjectReservations", x => x.Id);
                    table.CheckConstraint("CK_MediaObjectReservations_Bytes", "[ByteSize] >= 0");
                    table.CheckConstraint("CK_MediaObjectReservations_State", "[State] IN ('Reserved','Unknown','Stored','Deleting','Deleted')");
                });

            migrationBuilder.CreateTable(
                name: "MediaStorageStates",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    ScopeHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ChargedBytes = table.Column<long>(type: "bigint", nullable: false),
                    Initialized = table.Column<bool>(type: "bit", nullable: false),
                    UploadsPaused = table.Column<bool>(type: "bit", nullable: false),
                    LastReconciledAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReconcileToken = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReconcileUntilUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaStorageStates", x => x.Id);
                    table.CheckConstraint("CK_MediaStorageStates_Bytes", "[ChargedBytes] >= 0 AND [Id] = 1");
                });

            migrationBuilder.CreateTable(
                name: "MediaUsageCounters",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Count = table.Column<long>(type: "bigint", nullable: false),
                    Bytes = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaUsageCounters", x => x.Id);
                    table.CheckConstraint("CK_MediaUsageCounters_NonNegative", "[Count] >= 0 AND [Bytes] >= 0");
                });

            migrationBuilder.CreateIndex(
                name: "IX_MediaObjectReservations_State_CreatedAtUtc",
                table: "MediaObjectReservations",
                columns: new[] { "State", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_MediaObjectReservations_StorageKey",
                table: "MediaObjectReservations",
                column: "StorageKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MediaObjectReservations");

            migrationBuilder.DropTable(
                name: "MediaStorageStates");

            migrationBuilder.DropTable(
                name: "MediaUsageCounters");
        }
    }
}
