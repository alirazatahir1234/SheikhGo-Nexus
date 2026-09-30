using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SheikhGoNexusERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFarmQualityPhase7 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "farm");

            migrationBuilder.EnsureSchema(
                name: "quality");

            migrationBuilder.AddColumn<int>(
                name: "QualityStatus",
                schema: "inventory",
                table: "WarehouseStocks",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Farms",
                schema: "farm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FarmType = table.Column<int>(type: "int", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Farms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Farms_Branches_BranchId",
                        column: x => x.BranchId,
                        principalSchema: "core",
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LabTestTypes",
                schema: "quality",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MinValue = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    MaxValue = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LabTestTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FarmUnits",
                schema: "farm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FarmId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Capacity = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FarmUnits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FarmUnits_Farms_FarmId",
                        column: x => x.FarmId,
                        principalSchema: "farm",
                        principalTable: "Farms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Flocks",
                schema: "farm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FarmId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FarmUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BatchNo = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    BirdType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PlacementDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    QtyPlaced = table.Column<int>(type: "int", nullable: false),
                    QtyCurrent = table.Column<int>(type: "int", nullable: false),
                    QtyMortality = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Flocks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Flocks_FarmUnits_FarmUnitId",
                        column: x => x.FarmUnitId,
                        principalSchema: "farm",
                        principalTable: "FarmUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Flocks_Farms_FarmId",
                        column: x => x.FarmId,
                        principalSchema: "farm",
                        principalTable: "Farms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FarmExpenses",
                schema: "farm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ExpenseNo = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ExpenseDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FarmId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FlockId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Category = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CashBankAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Narration = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FarmExpenses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FarmExpenses_CashBankAccounts_CashBankAccountId",
                        column: x => x.CashBankAccountId,
                        principalSchema: "finance",
                        principalTable: "CashBankAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FarmExpenses_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalSchema: "documents",
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FarmExpenses_Farms_FarmId",
                        column: x => x.FarmId,
                        principalSchema: "farm",
                        principalTable: "Farms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FarmExpenses_Flocks_FlockId",
                        column: x => x.FlockId,
                        principalSchema: "farm",
                        principalTable: "Flocks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FarmTreatments",
                schema: "farm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TreatmentNo = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FlockId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Qty = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BatchNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Narration = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FarmTreatments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FarmTreatments_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalSchema: "documents",
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FarmTreatments_Flocks_FlockId",
                        column: x => x.FlockId,
                        principalSchema: "farm",
                        principalTable: "Flocks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FarmTreatments_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "inventory",
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FarmTreatments_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "inventory",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FeedIssues",
                schema: "farm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IssueNo = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    IssueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FarmId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FlockId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeedIssues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FeedIssues_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalSchema: "documents",
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FeedIssues_Farms_FarmId",
                        column: x => x.FarmId,
                        principalSchema: "farm",
                        principalTable: "Farms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FeedIssues_Flocks_FlockId",
                        column: x => x.FlockId,
                        principalSchema: "farm",
                        principalTable: "Flocks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FeedIssues_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "inventory",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LabSamples",
                schema: "quality",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SampleNo = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SampleDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SourceType = table.Column<int>(type: "int", nullable: false),
                    SourceEntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FlockId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LabSamples", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LabSamples_Flocks_FlockId",
                        column: x => x.FlockId,
                        principalSchema: "farm",
                        principalTable: "Flocks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LabSamples_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "inventory",
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LabSamples_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "inventory",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MortalityRecords",
                schema: "farm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FlockId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Qty = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RecordedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MortalityRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MortalityRecords_Flocks_FlockId",
                        column: x => x.FlockId,
                        principalSchema: "farm",
                        principalTable: "Flocks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MortalityRecords_Users_RecordedByUserId",
                        column: x => x.RecordedByUserId,
                        principalSchema: "identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FeedIssueLines",
                schema: "farm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FeedIssueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LineNo = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Qty = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeedIssueLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FeedIssueLines_FeedIssues_FeedIssueId",
                        column: x => x.FeedIssueId,
                        principalSchema: "farm",
                        principalTable: "FeedIssues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FeedIssueLines_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "inventory",
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LabTestResults",
                schema: "quality",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SampleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TestTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TestedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Value = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Decision = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ApprovedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Narration = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LabTestResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LabTestResults_LabSamples_SampleId",
                        column: x => x.SampleId,
                        principalSchema: "quality",
                        principalTable: "LabSamples",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LabTestResults_LabTestTypes_TestTypeId",
                        column: x => x.TestTypeId,
                        principalSchema: "quality",
                        principalTable: "LabTestTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LabTestResults_Users_ApprovedByUserId",
                        column: x => x.ApprovedByUserId,
                        principalSchema: "identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FarmExpenses_CashBankAccountId",
                schema: "farm",
                table: "FarmExpenses",
                column: "CashBankAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_FarmExpenses_DocumentId",
                schema: "farm",
                table: "FarmExpenses",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_FarmExpenses_FarmId",
                schema: "farm",
                table: "FarmExpenses",
                column: "FarmId");

            migrationBuilder.CreateIndex(
                name: "IX_FarmExpenses_FlockId",
                schema: "farm",
                table: "FarmExpenses",
                column: "FlockId");

            migrationBuilder.CreateIndex(
                name: "IX_FarmExpenses_TenantId_ExpenseNo",
                schema: "farm",
                table: "FarmExpenses",
                columns: new[] { "TenantId", "ExpenseNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Farms_BranchId",
                schema: "farm",
                table: "Farms",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_Farms_TenantId_Code",
                schema: "farm",
                table: "Farms",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FarmTreatments_DocumentId",
                schema: "farm",
                table: "FarmTreatments",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_FarmTreatments_FlockId",
                schema: "farm",
                table: "FarmTreatments",
                column: "FlockId");

            migrationBuilder.CreateIndex(
                name: "IX_FarmTreatments_ProductId",
                schema: "farm",
                table: "FarmTreatments",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_FarmTreatments_TenantId_TreatmentNo",
                schema: "farm",
                table: "FarmTreatments",
                columns: new[] { "TenantId", "TreatmentNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FarmTreatments_WarehouseId",
                schema: "farm",
                table: "FarmTreatments",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_FarmUnits_FarmId",
                schema: "farm",
                table: "FarmUnits",
                column: "FarmId");

            migrationBuilder.CreateIndex(
                name: "IX_FarmUnits_TenantId_FarmId_Code",
                schema: "farm",
                table: "FarmUnits",
                columns: new[] { "TenantId", "FarmId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FeedIssueLines_FeedIssueId",
                schema: "farm",
                table: "FeedIssueLines",
                column: "FeedIssueId");

            migrationBuilder.CreateIndex(
                name: "IX_FeedIssueLines_ProductId",
                schema: "farm",
                table: "FeedIssueLines",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_FeedIssues_DocumentId",
                schema: "farm",
                table: "FeedIssues",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_FeedIssues_FarmId",
                schema: "farm",
                table: "FeedIssues",
                column: "FarmId");

            migrationBuilder.CreateIndex(
                name: "IX_FeedIssues_FlockId",
                schema: "farm",
                table: "FeedIssues",
                column: "FlockId");

            migrationBuilder.CreateIndex(
                name: "IX_FeedIssues_TenantId_IssueNo",
                schema: "farm",
                table: "FeedIssues",
                columns: new[] { "TenantId", "IssueNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FeedIssues_WarehouseId",
                schema: "farm",
                table: "FeedIssues",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_Flocks_FarmId",
                schema: "farm",
                table: "Flocks",
                column: "FarmId");

            migrationBuilder.CreateIndex(
                name: "IX_Flocks_FarmUnitId",
                schema: "farm",
                table: "Flocks",
                column: "FarmUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_Flocks_TenantId_BatchNo",
                schema: "farm",
                table: "Flocks",
                columns: new[] { "TenantId", "BatchNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LabSamples_FlockId",
                schema: "quality",
                table: "LabSamples",
                column: "FlockId");

            migrationBuilder.CreateIndex(
                name: "IX_LabSamples_ProductId",
                schema: "quality",
                table: "LabSamples",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_LabSamples_TenantId_SampleNo",
                schema: "quality",
                table: "LabSamples",
                columns: new[] { "TenantId", "SampleNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LabSamples_WarehouseId",
                schema: "quality",
                table: "LabSamples",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_LabTestResults_ApprovedByUserId",
                schema: "quality",
                table: "LabTestResults",
                column: "ApprovedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_LabTestResults_SampleId",
                schema: "quality",
                table: "LabTestResults",
                column: "SampleId");

            migrationBuilder.CreateIndex(
                name: "IX_LabTestResults_TestTypeId",
                schema: "quality",
                table: "LabTestResults",
                column: "TestTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_LabTestTypes_TenantId_Code",
                schema: "quality",
                table: "LabTestTypes",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MortalityRecords_FlockId",
                schema: "farm",
                table: "MortalityRecords",
                column: "FlockId");

            migrationBuilder.CreateIndex(
                name: "IX_MortalityRecords_RecordedByUserId",
                schema: "farm",
                table: "MortalityRecords",
                column: "RecordedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FarmExpenses",
                schema: "farm");

            migrationBuilder.DropTable(
                name: "FarmTreatments",
                schema: "farm");

            migrationBuilder.DropTable(
                name: "FeedIssueLines",
                schema: "farm");

            migrationBuilder.DropTable(
                name: "LabTestResults",
                schema: "quality");

            migrationBuilder.DropTable(
                name: "MortalityRecords",
                schema: "farm");

            migrationBuilder.DropTable(
                name: "FeedIssues",
                schema: "farm");

            migrationBuilder.DropTable(
                name: "LabSamples",
                schema: "quality");

            migrationBuilder.DropTable(
                name: "LabTestTypes",
                schema: "quality");

            migrationBuilder.DropTable(
                name: "Flocks",
                schema: "farm");

            migrationBuilder.DropTable(
                name: "FarmUnits",
                schema: "farm");

            migrationBuilder.DropTable(
                name: "Farms",
                schema: "farm");

            migrationBuilder.DropColumn(
                name: "QualityStatus",
                schema: "inventory",
                table: "WarehouseStocks");
        }
    }
}
