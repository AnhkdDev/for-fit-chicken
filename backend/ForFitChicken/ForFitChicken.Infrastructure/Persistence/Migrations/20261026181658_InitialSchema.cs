using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ForFitChicken.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DisplayName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    ShortDescription = table.Column<string>(type: "nvarchar(600)", maxLength: 600, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Ingredients = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NutritionText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UsageInstructions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StorageInstructions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsFeatured = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    FirstPublishedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    Version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                    table.CheckConstraint("CK_Products_SortOrder", "[SortOrder] >= 0");
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<long>(type: "bigint", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    RoleId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ActorUserId = table.Column<long>(type: "bigint", nullable: true),
                    Action = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    EntityType = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    EntityId = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    CorrelationId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    ChangesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                    table.CheckConstraint("CK_AuditLogs_Json", "[ChangesJson] IS NULL OR ISJSON([ChangesJson]) = 1");
                    table.ForeignKey(
                        name: "FK_AuditLogs_AspNetUsers_ActorUserId",
                        column: x => x.ActorUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "MediaAssets",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StorageKey = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ContentType = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    ByteSize = table.Column<long>(type: "bigint", nullable: false),
                    Width = table.Column<int>(type: "int", nullable: true),
                    Height = table.Column<int>(type: "int", nullable: true),
                    DefaultAlt = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    CreatedByUserId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaAssets", x => x.Id);
                    table.CheckConstraint("CK_MediaAssets_Size", "[ByteSize] > 0 AND ([Width] IS NULL OR [Width] > 0) AND ([Height] IS NULL OR [Height] > 0)");
                    table.ForeignKey(
                        name: "FK_MediaAssets_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderNumber = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false),
                    Source = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    PaymentMethod = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false, defaultValue: "COD"),
                    RecipientName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    RecipientPhone = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    RecipientAddress = table.Column<string>(type: "nvarchar(600)", maxLength: 600, nullable: false),
                    CustomerNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    InternalNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ItemsSubtotal = table.Column<decimal>(type: "decimal(18,0)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    CreatedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    ConfirmedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    ConfirmedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    HandedOverAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    CancelledAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    CancellationReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ReplacesOrderId = table.Column<long>(type: "bigint", nullable: true),
                    Version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.Id);
                    table.CheckConstraint("CK_Orders_NotSelfReplacement", "[ReplacesOrderId] IS NULL OR [ReplacesOrderId] <> [Id]");
                    table.CheckConstraint("CK_Orders_PaymentMethod", "[PaymentMethod] IN ('COD')");
                    table.CheckConstraint("CK_Orders_Source", "[Source] IN ('Website', 'Facebook', 'Zalo', 'Phone', 'Other')");
                    table.CheckConstraint("CK_Orders_Status", "[Status] IN ('PendingConfirmation', 'Confirmed', 'Preparing', 'HandedOver', 'Completed', 'Cancelled')");
                    table.CheckConstraint("CK_Orders_Subtotal", "[ItemsSubtotal] >= 0");
                    table.ForeignKey(
                        name: "FK_Orders_AspNetUsers_ConfirmedByUserId",
                        column: x => x.ConfirmedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Orders_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Orders_Orders_ReplacesOrderId",
                        column: x => x.ReplacesOrderId,
                        principalTable: "Orders",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SiteContent",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SectionKey = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    SchemaVersion = table.Column<int>(type: "int", nullable: false),
                    DraftJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PublishedJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PublishedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    PublishedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    UpdatedByUserId = table.Column<long>(type: "bigint", nullable: false),
                    Version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiteContent", x => x.Id);
                    table.CheckConstraint("CK_SiteContent_Json", "[SchemaVersion] > 0 AND ISJSON([DraftJson]) = 1 AND ([PublishedJson] IS NULL OR ISJSON([PublishedJson]) = 1)");
                    table.CheckConstraint("CK_SiteContent_Publication", "([PublishedJson] IS NULL AND [PublishedAtUtc] IS NULL AND [PublishedByUserId] IS NULL) OR ([PublishedJson] IS NOT NULL AND [PublishedAtUtc] IS NOT NULL AND [PublishedByUserId] IS NOT NULL)");
                    table.CheckConstraint("CK_SiteContent_Section", "[SectionKey] IN ('HomeHero', 'Story', 'Usage', 'Faq', 'Contact', 'SiteSettings')");
                    table.ForeignKey(
                        name: "FK_SiteContent_AspNetUsers_PublishedByUserId",
                        column: x => x.PublishedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SiteContent_AspNetUsers_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ProductVariants",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<long>(type: "bigint", nullable: false),
                    SkuCode = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    Label = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    NetWeightGrams = table.Column<int>(type: "int", nullable: true),
                    SellingPrice = table.Column<decimal>(type: "decimal(18,0)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    Version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductVariants", x => x.Id);
                    table.UniqueConstraint("AK_ProductVariants_ProductId_Id", x => new { x.ProductId, x.Id });
                    table.CheckConstraint("CK_ProductVariants_PriceWeight", "[SellingPrice] > 0 AND ([NetWeightGrams] IS NULL OR [NetWeightGrams] > 0) AND ([IsActive] = 0 OR [NetWeightGrams] IS NOT NULL)");
                    table.CheckConstraint("CK_ProductVariants_SortOrder", "[SortOrder] >= 0");
                    table.ForeignKey(
                        name: "FK_ProductVariants_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Gyms",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GymCode = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PublicAddress = table.Column<string>(type: "nvarchar(600)", maxLength: 600, nullable: false),
                    PublicPhone = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: true),
                    PublicDescription = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    InternalContactName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    InternalPhone = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: true),
                    InternalNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CoverAssetId = table.Column<long>(type: "bigint", nullable: true),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    Version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Gyms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Gyms_MediaAssets_CoverAssetId",
                        column: x => x.CoverAssetId,
                        principalTable: "MediaAssets",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CodReceipts",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<long>(type: "bigint", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,0)", nullable: false),
                    ReceivedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    RecordedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    RecordedByUserId = table.Column<long>(type: "bigint", nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    VoidedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    VoidedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    VoidReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CodReceipts", x => x.Id);
                    table.CheckConstraint("CK_CodReceipts_Amount", "[Amount] > 0");
                    table.CheckConstraint("CK_CodReceipts_Void", "([VoidedAtUtc] IS NULL AND [VoidedByUserId] IS NULL AND [VoidReason] IS NULL) OR ([VoidedAtUtc] IS NOT NULL AND [VoidedByUserId] IS NOT NULL AND [VoidReason] IS NOT NULL AND LEN(LTRIM(RTRIM([VoidReason]))) > 0)");
                    table.ForeignKey(
                        name: "FK_CodReceipts_AspNetUsers_RecordedByUserId",
                        column: x => x.RecordedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CodReceipts_AspNetUsers_VoidedByUserId",
                        column: x => x.VoidedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CodReceipts_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "IdempotencyRequests",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ScopeHash = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    Operation = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    RequestKey = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    RequestHash = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    OrderId = table.Column<long>(type: "bigint", nullable: true),
                    ResponseStatus = table.Column<int>(type: "int", nullable: false),
                    SafeResponseJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdempotencyRequests", x => x.Id);
                    table.CheckConstraint("CK_IdempotencyRequests_Response", "[ResponseStatus] BETWEEN 100 AND 599 AND [ExpiresAtUtc] > [CreatedAtUtc] AND LEN([ScopeHash]) = 64 AND LEN([RequestHash]) = 64 AND ([SafeResponseJson] IS NULL OR ISJSON([SafeResponseJson]) = 1)");
                    table.ForeignKey(
                        name: "FK_IdempotencyRequests_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "NotificationJobs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<long>(type: "bigint", nullable: false),
                    EventType = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false),
                    Channel = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    TargetChatId = table.Column<long>(type: "bigint", nullable: false),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    AttemptCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    NextAttemptAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    LeaseToken = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LeaseUntilUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    LastError = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    TelegramMessageId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    SentAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationJobs", x => x.Id);
                    table.CheckConstraint("CK_NotificationJobs_Channel", "[Channel] IN ('Telegram')");
                    table.CheckConstraint("CK_NotificationJobs_EventType", "[EventType] IN ('OrderRequested')");
                    table.CheckConstraint("CK_NotificationJobs_Lease", "([Status] = 'Processing' AND [LeaseToken] IS NOT NULL AND [LeaseUntilUtc] IS NOT NULL) OR ([Status] <> 'Processing' AND [LeaseToken] IS NULL AND [LeaseUntilUtc] IS NULL)");
                    table.CheckConstraint("CK_NotificationJobs_Payload", "ISJSON([PayloadJson]) = 1 AND [AttemptCount] >= 0");
                    table.CheckConstraint("CK_NotificationJobs_Sent", "([Status] = 'Sent' AND [SentAtUtc] IS NOT NULL) OR ([Status] <> 'Sent' AND [SentAtUtc] IS NULL)");
                    table.CheckConstraint("CK_NotificationJobs_Status", "[Status] IN ('Pending', 'Processing', 'Sent', 'Failed')");
                    table.ForeignKey(
                        name: "FK_NotificationJobs_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "OrderEvents",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<long>(type: "bigint", nullable: false),
                    EventType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    FromStatus = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: true),
                    ToStatus = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ChangedFieldsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    ActorUserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderEvents", x => x.Id);
                    table.CheckConstraint("CK_OrderEvents_FromStatus", "[FromStatus] IS NULL OR [FromStatus] IN ('PendingConfirmation', 'Confirmed', 'Preparing', 'HandedOver', 'Completed', 'Cancelled')");
                    table.CheckConstraint("CK_OrderEvents_Json", "[ChangedFieldsJson] IS NULL OR ISJSON([ChangedFieldsJson]) = 1");
                    table.CheckConstraint("CK_OrderEvents_ToStatus", "[ToStatus] IS NULL OR [ToStatus] IN ('PendingConfirmation', 'Confirmed', 'Preparing', 'HandedOver', 'Completed', 'Cancelled')");
                    table.ForeignKey(
                        name: "FK_OrderEvents_AspNetUsers_ActorUserId",
                        column: x => x.ActorUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OrderEvents_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "StockDocuments",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DocumentNo = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    OperationKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentType = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    OrderId = table.Column<long>(type: "bigint", nullable: true),
                    CorrectsDocumentId = table.Column<long>(type: "bigint", nullable: true),
                    EffectiveAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    CreatedByUserId = table.Column<long>(type: "bigint", nullable: false),
                    PostedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    PostedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockDocuments", x => x.Id);
                    table.CheckConstraint("CK_StockDocuments_DocumentType", "[DocumentType] IN ('ProductionReceipt', 'GymDelivery', 'GymRecall', 'Reclassify', 'WriteOff', 'Adjustment', 'OrderIssue')");
                    table.CheckConstraint("CK_StockDocuments_NotSelfCorrection", "[CorrectsDocumentId] IS NULL OR [CorrectsDocumentId] <> [Id]");
                    table.CheckConstraint("CK_StockDocuments_OrderReference", "([DocumentType] = 'OrderIssue' AND [OrderId] IS NOT NULL) OR ([DocumentType] <> 'OrderIssue' AND [OrderId] IS NULL)");
                    table.CheckConstraint("CK_StockDocuments_Posting", "([Status] = 'Posted' AND [PostedAtUtc] IS NOT NULL AND [PostedByUserId] IS NOT NULL) OR ([Status] <> 'Posted' AND [PostedAtUtc] IS NULL AND [PostedByUserId] IS NULL)");
                    table.CheckConstraint("CK_StockDocuments_Status", "[Status] IN ('Draft', 'Posted', 'Cancelled')");
                    table.ForeignKey(
                        name: "FK_StockDocuments_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StockDocuments_AspNetUsers_PostedByUserId",
                        column: x => x.PostedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StockDocuments_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StockDocuments_StockDocuments_CorrectsDocumentId",
                        column: x => x.CorrectsDocumentId,
                        principalTable: "StockDocuments",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "OrderItems",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<long>(type: "bigint", nullable: false),
                    VariantId = table.Column<long>(type: "bigint", nullable: false),
                    SkuCodeSnapshot = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    ProductNameSnapshot = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    VariantLabelSnapshot = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    NetWeightGramsSnapshot = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,0)", nullable: false),
                    LineTotal = table.Column<decimal>(type: "decimal(18,0)", nullable: false, computedColumnSql: "CONVERT(decimal(18,0), [UnitPrice] * [Quantity])", stored: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderItems", x => x.Id);
                    table.CheckConstraint("CK_OrderItems_Quantities", "[Quantity] > 0 AND [UnitPrice] >= 0 AND [NetWeightGramsSnapshot] > 0");
                    table.ForeignKey(
                        name: "FK_OrderItems_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OrderItems_ProductVariants_VariantId",
                        column: x => x.VariantId,
                        principalTable: "ProductVariants",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ProductBatches",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VariantId = table.Column<long>(type: "bigint", nullable: false),
                    BatchCode = table.Column<string>(type: "varchar(60)", maxLength: 60, nullable: false),
                    ManufacturedOn = table.Column<DateOnly>(type: "date", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    IsSaleBlocked = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    BlockReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    BlockedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    BlockedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    CreatedByUserId = table.Column<long>(type: "bigint", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    Version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductBatches", x => x.Id);
                    table.CheckConstraint("CK_ProductBatches_Blocked", "[IsSaleBlocked] = 0 OR ([BlockReason] IS NOT NULL AND LEN(LTRIM(RTRIM([BlockReason]))) > 0 AND [BlockedAtUtc] IS NOT NULL AND [BlockedByUserId] IS NOT NULL)");
                    table.CheckConstraint("CK_ProductBatches_Cost", "[UnitCost] IS NULL OR [UnitCost] >= 0");
                    table.ForeignKey(
                        name: "FK_ProductBatches_AspNetUsers_BlockedByUserId",
                        column: x => x.BlockedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductBatches_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductBatches_ProductVariants_VariantId",
                        column: x => x.VariantId,
                        principalTable: "ProductVariants",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ProductImages",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<long>(type: "bigint", nullable: false),
                    VariantId = table.Column<long>(type: "bigint", nullable: true),
                    AssetId = table.Column<long>(type: "bigint", nullable: false),
                    AltText = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductImages", x => x.Id);
                    table.CheckConstraint("CK_ProductImages_SortOrder", "[SortOrder] >= 0");
                    table.ForeignKey(
                        name: "FK_ProductImages_MediaAssets_AssetId",
                        column: x => x.AssetId,
                        principalTable: "MediaAssets",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductImages_ProductVariants_ProductId_VariantId",
                        columns: x => new { x.ProductId, x.VariantId },
                        principalTable: "ProductVariants",
                        principalColumns: new[] { "ProductId", "Id" });
                    table.ForeignKey(
                        name: "FK_ProductImages_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "GymStockCounts",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CountNumber = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    GymId = table.Column<long>(type: "bigint", nullable: false),
                    PreviousCountId = table.Column<long>(type: "bigint", nullable: true),
                    Kind = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    CountedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    CountedByUserId = table.Column<long>(type: "bigint", nullable: false),
                    SourceFingerprint = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    CreatedByUserId = table.Column<long>(type: "bigint", nullable: false),
                    PostedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    PostedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GymStockCounts", x => x.Id);
                    table.UniqueConstraint("AK_GymStockCounts_GymId_Id", x => new { x.GymId, x.Id });
                    table.CheckConstraint("CK_GymStockCounts_Kind", "[Kind] IN ('Regular', 'OpeningBaseline')");
                    table.CheckConstraint("CK_GymStockCounts_NotSelfPrevious", "[PreviousCountId] IS NULL OR [PreviousCountId] <> [Id]");
                    table.CheckConstraint("CK_GymStockCounts_Posting", "([Status] = 'Posted' AND [PostedAtUtc] IS NOT NULL AND [PostedByUserId] IS NOT NULL) OR ([Status] <> 'Posted' AND [PostedAtUtc] IS NULL AND [PostedByUserId] IS NULL)");
                    table.CheckConstraint("CK_GymStockCounts_Status", "[Status] IN ('Draft', 'Posted', 'Cancelled')");
                    table.ForeignKey(
                        name: "FK_GymStockCounts_AspNetUsers_CountedByUserId",
                        column: x => x.CountedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_GymStockCounts_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_GymStockCounts_AspNetUsers_PostedByUserId",
                        column: x => x.PostedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_GymStockCounts_GymStockCounts_GymId_PreviousCountId",
                        columns: x => new { x.GymId, x.PreviousCountId },
                        principalTable: "GymStockCounts",
                        principalColumns: new[] { "GymId", "Id" });
                    table.ForeignKey(
                        name: "FK_GymStockCounts_Gyms_GymId",
                        column: x => x.GymId,
                        principalTable: "Gyms",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "StockLocations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LocationCode = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Kind = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    GymId = table.Column<long>(type: "bigint", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    Version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockLocations", x => x.Id);
                    table.CheckConstraint("CK_StockLocations_GymKind", "([Kind] = 'Gym' AND [GymId] IS NOT NULL) OR ([Kind] = 'Warehouse' AND [GymId] IS NULL)");
                    table.CheckConstraint("CK_StockLocations_Kind", "[Kind] IN ('Warehouse', 'Gym')");
                    table.ForeignKey(
                        name: "FK_StockLocations_Gyms_GymId",
                        column: x => x.GymId,
                        principalTable: "Gyms",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "GymStockCountLines",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CountId = table.Column<long>(type: "bigint", nullable: false),
                    BatchId = table.Column<long>(type: "bigint", nullable: false),
                    ExpectedGood = table.Column<int>(type: "int", nullable: true),
                    ExpectedQuarantine = table.Column<int>(type: "int", nullable: true),
                    ExpectedDamaged = table.Column<int>(type: "int", nullable: true),
                    ActualGood = table.Column<int>(type: "int", nullable: true),
                    ActualQuarantine = table.Column<int>(type: "int", nullable: true),
                    ActualDamaged = table.Column<int>(type: "int", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GymStockCountLines", x => x.Id);
                    table.CheckConstraint("CK_GymStockCountLines_Actual", "([ActualGood] IS NULL OR [ActualGood] >= 0) AND ([ActualQuarantine] IS NULL OR [ActualQuarantine] >= 0) AND ([ActualDamaged] IS NULL OR [ActualDamaged] >= 0)");
                    table.ForeignKey(
                        name: "FK_GymStockCountLines_GymStockCounts_CountId",
                        column: x => x.CountId,
                        principalTable: "GymStockCounts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_GymStockCountLines_ProductBatches_BatchId",
                        column: x => x.BatchId,
                        principalTable: "ProductBatches",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "StockDocumentLines",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DocumentId = table.Column<long>(type: "bigint", nullable: false),
                    BatchId = table.Column<long>(type: "bigint", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    FromLocationId = table.Column<long>(type: "bigint", nullable: true),
                    FromCondition = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true),
                    ToLocationId = table.Column<long>(type: "bigint", nullable: true),
                    ToCondition = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockDocumentLines", x => x.Id);
                    table.CheckConstraint("CK_StockDocumentLines_Endpoints", "([FromLocationId] IS NOT NULL OR [ToLocationId] IS NOT NULL) AND (([FromLocationId] IS NULL AND [FromCondition] IS NULL) OR ([FromLocationId] IS NOT NULL AND [FromCondition] IS NOT NULL)) AND (([ToLocationId] IS NULL AND [ToCondition] IS NULL) OR ([ToLocationId] IS NOT NULL AND [ToCondition] IS NOT NULL)) AND ([FromLocationId] IS NULL OR [ToLocationId] IS NULL OR [FromLocationId] <> [ToLocationId] OR [FromCondition] <> [ToCondition])");
                    table.CheckConstraint("CK_StockDocumentLines_FromCondition", "[FromCondition] IS NULL OR [FromCondition] IN ('Good', 'Quarantine', 'Damaged')");
                    table.CheckConstraint("CK_StockDocumentLines_Quantity", "[Quantity] > 0");
                    table.CheckConstraint("CK_StockDocumentLines_ToCondition", "[ToCondition] IS NULL OR [ToCondition] IN ('Good', 'Quarantine', 'Damaged')");
                    table.ForeignKey(
                        name: "FK_StockDocumentLines_ProductBatches_BatchId",
                        column: x => x.BatchId,
                        principalTable: "ProductBatches",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StockDocumentLines_StockDocuments_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "StockDocuments",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StockDocumentLines_StockLocations_FromLocationId",
                        column: x => x.FromLocationId,
                        principalTable: "StockLocations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StockDocumentLines_StockLocations_ToLocationId",
                        column: x => x.ToLocationId,
                        principalTable: "StockLocations",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "WarehouseBalances",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LocationId = table.Column<long>(type: "bigint", nullable: false),
                    BatchId = table.Column<long>(type: "bigint", nullable: false),
                    Condition = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    OnHand = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    Reserved = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    Version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarehouseBalances", x => x.Id);
                    table.CheckConstraint("CK_WarehouseBalances_Condition", "[Condition] IN ('Good', 'Quarantine', 'Damaged')");
                    table.CheckConstraint("CK_WarehouseBalances_Quantities", "[OnHand] >= 0 AND [Reserved] >= 0 AND [Reserved] <= [OnHand] AND ([Condition] = 'Good' OR [Reserved] = 0)");
                    table.ForeignKey(
                        name: "FK_WarehouseBalances_ProductBatches_BatchId",
                        column: x => x.BatchId,
                        principalTable: "ProductBatches",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WarehouseBalances_StockLocations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "StockLocations",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "GymVarianceResolutions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CountLineId = table.Column<long>(type: "bigint", nullable: false),
                    Direction = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false),
                    Category = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    EvidenceReference = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    VerifiedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    CreatedByUserId = table.Column<long>(type: "bigint", nullable: false),
                    VoidedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    VoidedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    VoidReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GymVarianceResolutions", x => x.Id);
                    table.CheckConstraint("CK_GymVarianceResolutions_Category", "[Category] IN ('Sale', 'Loss', 'Other')");
                    table.CheckConstraint("CK_GymVarianceResolutions_Direction", "[Direction] IN ('Missing', 'Surplus')");
                    table.CheckConstraint("CK_GymVarianceResolutions_Resolution", "[Quantity] > 0 AND LEN(LTRIM(RTRIM([Reason]))) > 0 AND ([Category] <> 'Sale' OR [Direction] = 'Missing')");
                    table.CheckConstraint("CK_GymVarianceResolutions_Void", "([VoidedAtUtc] IS NULL AND [VoidedByUserId] IS NULL AND [VoidReason] IS NULL) OR ([VoidedAtUtc] IS NOT NULL AND [VoidedByUserId] IS NOT NULL AND [VoidReason] IS NOT NULL AND LEN(LTRIM(RTRIM([VoidReason]))) > 0)");
                    table.ForeignKey(
                        name: "FK_GymVarianceResolutions_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_GymVarianceResolutions_AspNetUsers_VoidedByUserId",
                        column: x => x.VoidedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_GymVarianceResolutions_GymStockCountLines_CountLineId",
                        column: x => x.CountLineId,
                        principalTable: "GymStockCountLines",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "OrderAllocations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderItemId = table.Column<long>(type: "bigint", nullable: false),
                    BalanceId = table.Column<long>(type: "bigint", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    State = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    UnitCostSnapshot = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    ReservedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    IssuedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    ReleasedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    ReleaseReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderAllocations", x => x.Id);
                    table.CheckConstraint("CK_OrderAllocations_QuantityCost", "[Quantity] > 0 AND ([UnitCostSnapshot] IS NULL OR [UnitCostSnapshot] >= 0)");
                    table.CheckConstraint("CK_OrderAllocations_State", "[State] IN ('Reserved', 'Issued', 'Released')");
                    table.CheckConstraint("CK_OrderAllocations_StateTimes", "([State] = 'Reserved' AND [IssuedAtUtc] IS NULL AND [ReleasedAtUtc] IS NULL) OR ([State] = 'Issued' AND [IssuedAtUtc] IS NOT NULL AND [ReleasedAtUtc] IS NULL) OR ([State] = 'Released' AND [IssuedAtUtc] IS NULL AND [ReleasedAtUtc] IS NOT NULL AND [ReleaseReason] IS NOT NULL AND LEN(LTRIM(RTRIM([ReleaseReason]))) > 0)");
                    table.ForeignKey(
                        name: "FK_OrderAllocations_OrderItems_OrderItemId",
                        column: x => x.OrderItemId,
                        principalTable: "OrderItems",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OrderAllocations_WarehouseBalances_BalanceId",
                        column: x => x.BalanceId,
                        principalTable: "WarehouseBalances",
                        principalColumn: "Id");
                });

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { 1L, "role-owner-v1", "Owner", "OWNER" },
                    { 2L, "role-operator-v1", "Operator", "OPERATOR" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_ActorUserId",
                table: "AuditLogs",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_CreatedAtUtc",
                table: "AuditLogs",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EntityType_EntityId_Id",
                table: "AuditLogs",
                columns: new[] { "EntityType", "EntityId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_CodReceipts_OrderId",
                table: "CodReceipts",
                column: "OrderId",
                unique: true,
                filter: "[VoidedAtUtc] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CodReceipts_RecordedByUserId",
                table: "CodReceipts",
                column: "RecordedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CodReceipts_VoidedByUserId",
                table: "CodReceipts",
                column: "VoidedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Gyms_CoverAssetId",
                table: "Gyms",
                column: "CoverAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_Gyms_GymCode",
                table: "Gyms",
                column: "GymCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GymStockCountLines_BatchId",
                table: "GymStockCountLines",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_GymStockCountLines_CountId_BatchId",
                table: "GymStockCountLines",
                columns: new[] { "CountId", "BatchId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GymStockCounts_CountedByUserId",
                table: "GymStockCounts",
                column: "CountedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_GymStockCounts_CountNumber",
                table: "GymStockCounts",
                column: "CountNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GymStockCounts_CreatedByUserId",
                table: "GymStockCounts",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_GymStockCounts_GymId",
                table: "GymStockCounts",
                column: "GymId",
                unique: true,
                filter: "[Status] = 'Posted' AND [PreviousCountId] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_GymStockCounts_GymId_CountedAtUtc",
                table: "GymStockCounts",
                columns: new[] { "GymId", "CountedAtUtc" },
                unique: true,
                filter: "[Status] = 'Posted'");

            migrationBuilder.CreateIndex(
                name: "IX_GymStockCounts_GymId_PreviousCountId",
                table: "GymStockCounts",
                columns: new[] { "GymId", "PreviousCountId" });

            migrationBuilder.CreateIndex(
                name: "IX_GymStockCounts_GymId_Status_CountedAtUtc",
                table: "GymStockCounts",
                columns: new[] { "GymId", "Status", "CountedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_GymStockCounts_PostedByUserId",
                table: "GymStockCounts",
                column: "PostedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_GymStockCounts_PreviousCountId",
                table: "GymStockCounts",
                column: "PreviousCountId",
                unique: true,
                filter: "[Status] = 'Posted' AND [PreviousCountId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_GymVarianceResolutions_CountLineId_VoidedAtUtc",
                table: "GymVarianceResolutions",
                columns: new[] { "CountLineId", "VoidedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_GymVarianceResolutions_CreatedByUserId",
                table: "GymVarianceResolutions",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_GymVarianceResolutions_VoidedByUserId",
                table: "GymVarianceResolutions",
                column: "VoidedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_IdempotencyRequests_ExpiresAtUtc",
                table: "IdempotencyRequests",
                column: "ExpiresAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_IdempotencyRequests_OrderId",
                table: "IdempotencyRequests",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_IdempotencyRequests_ScopeHash_Operation_RequestKey",
                table: "IdempotencyRequests",
                columns: new[] { "ScopeHash", "Operation", "RequestKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MediaAssets_CreatedByUserId",
                table: "MediaAssets",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MediaAssets_StorageKey",
                table: "MediaAssets",
                column: "StorageKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NotificationJobs_OrderId_EventType_Channel_TargetChatId",
                table: "NotificationJobs",
                columns: new[] { "OrderId", "EventType", "Channel", "TargetChatId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NotificationJobs_Status_NextAttemptAtUtc",
                table: "NotificationJobs",
                columns: new[] { "Status", "NextAttemptAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_OrderAllocations_BalanceId_State",
                table: "OrderAllocations",
                columns: new[] { "BalanceId", "State" });

            migrationBuilder.CreateIndex(
                name: "IX_OrderAllocations_OrderItemId_BalanceId",
                table: "OrderAllocations",
                columns: new[] { "OrderItemId", "BalanceId" },
                unique: true,
                filter: "[State] = 'Reserved'");

            migrationBuilder.CreateIndex(
                name: "IX_OrderAllocations_OrderItemId_State",
                table: "OrderAllocations",
                columns: new[] { "OrderItemId", "State" });

            migrationBuilder.CreateIndex(
                name: "IX_OrderEvents_ActorUserId",
                table: "OrderEvents",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderEvents_OrderId_Id",
                table: "OrderEvents",
                columns: new[] { "OrderId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_OrderId_VariantId",
                table: "OrderItems",
                columns: new[] { "OrderId", "VariantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_VariantId",
                table: "OrderItems",
                column: "VariantId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_ConfirmedByUserId",
                table: "Orders",
                column: "ConfirmedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CreatedByUserId",
                table: "Orders",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_OrderNumber",
                table: "Orders",
                column: "OrderNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_ReplacesOrderId",
                table: "Orders",
                column: "ReplacesOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_Status_CreatedAtUtc",
                table: "Orders",
                columns: new[] { "Status", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductBatches_BatchCode",
                table: "ProductBatches",
                column: "BatchCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductBatches_BlockedByUserId",
                table: "ProductBatches",
                column: "BlockedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductBatches_CreatedByUserId",
                table: "ProductBatches",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductBatches_VariantId_ExpiresAtUtc_Id",
                table: "ProductBatches",
                columns: new[] { "VariantId", "ExpiresAtUtc", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductImages_AssetId",
                table: "ProductImages",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductImages_ProductId_SortOrder_Id",
                table: "ProductImages",
                columns: new[] { "ProductId", "SortOrder", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductImages_ProductId_VariantId",
                table: "ProductImages",
                columns: new[] { "ProductId", "VariantId" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_Slug",
                table: "Products",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_ProductId_Label",
                table: "ProductVariants",
                columns: new[] { "ProductId", "Label" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_SkuCode",
                table: "ProductVariants",
                column: "SkuCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SiteContent_PublishedByUserId",
                table: "SiteContent",
                column: "PublishedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SiteContent_SectionKey",
                table: "SiteContent",
                column: "SectionKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SiteContent_UpdatedByUserId",
                table: "SiteContent",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_StockDocumentLines_BatchId",
                table: "StockDocumentLines",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_StockDocumentLines_DocumentId_Id",
                table: "StockDocumentLines",
                columns: new[] { "DocumentId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_StockDocumentLines_FromLocationId_BatchId",
                table: "StockDocumentLines",
                columns: new[] { "FromLocationId", "BatchId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockDocumentLines_ToLocationId_BatchId",
                table: "StockDocumentLines",
                columns: new[] { "ToLocationId", "BatchId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockDocuments_CorrectsDocumentId",
                table: "StockDocuments",
                column: "CorrectsDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_StockDocuments_CreatedByUserId",
                table: "StockDocuments",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_StockDocuments_DocumentNo",
                table: "StockDocuments",
                column: "DocumentNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockDocuments_OperationKey",
                table: "StockDocuments",
                column: "OperationKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockDocuments_OrderId",
                table: "StockDocuments",
                column: "OrderId",
                unique: true,
                filter: "[DocumentType] = 'OrderIssue' AND [Status] = 'Posted' AND [OrderId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_StockDocuments_PostedByUserId",
                table: "StockDocuments",
                column: "PostedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_StockDocuments_Status_EffectiveAtUtc",
                table: "StockDocuments",
                columns: new[] { "Status", "EffectiveAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_StockLocations_GymId",
                table: "StockLocations",
                column: "GymId",
                unique: true,
                filter: "[GymId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_StockLocations_LocationCode",
                table: "StockLocations",
                column: "LocationCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseBalances_BatchId",
                table: "WarehouseBalances",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseBalances_LocationId_BatchId_Condition",
                table: "WarehouseBalances",
                columns: new[] { "LocationId", "BatchId", "Condition" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "CodReceipts");

            migrationBuilder.DropTable(
                name: "GymVarianceResolutions");

            migrationBuilder.DropTable(
                name: "IdempotencyRequests");

            migrationBuilder.DropTable(
                name: "NotificationJobs");

            migrationBuilder.DropTable(
                name: "OrderAllocations");

            migrationBuilder.DropTable(
                name: "OrderEvents");

            migrationBuilder.DropTable(
                name: "ProductImages");

            migrationBuilder.DropTable(
                name: "SiteContent");

            migrationBuilder.DropTable(
                name: "StockDocumentLines");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "GymStockCountLines");

            migrationBuilder.DropTable(
                name: "OrderItems");

            migrationBuilder.DropTable(
                name: "WarehouseBalances");

            migrationBuilder.DropTable(
                name: "StockDocuments");

            migrationBuilder.DropTable(
                name: "GymStockCounts");

            migrationBuilder.DropTable(
                name: "ProductBatches");

            migrationBuilder.DropTable(
                name: "StockLocations");

            migrationBuilder.DropTable(
                name: "Orders");

            migrationBuilder.DropTable(
                name: "ProductVariants");

            migrationBuilder.DropTable(
                name: "Gyms");

            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropTable(
                name: "MediaAssets");

            migrationBuilder.DropTable(
                name: "AspNetUsers");
        }
    }
}
