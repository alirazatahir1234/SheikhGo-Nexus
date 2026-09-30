using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SheikhGoNexusERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOperationsPosPhase8 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "operations");

            migrationBuilder.EnsureSchema(
                name: "pos");

            migrationBuilder.EnsureSchema(
                name: "weighbridge");

            migrationBuilder.CreateTable(
                name: "Drivers",
                schema: "operations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LicenseNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_Drivers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Drivers_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "hr",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PosTerminals",
                schema: "pos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                    table.PrimaryKey("PK_PosTerminals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PosTerminals_Branches_BranchId",
                        column: x => x.BranchId,
                        principalSchema: "core",
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PosTerminals_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "inventory",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VehicleTypes",
                schema: "operations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
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
                    table.PrimaryKey("PK_VehicleTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WeighbridgeScales",
                schema: "weighbridge",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Location = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_WeighbridgeScales", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PosShifts",
                schema: "pos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TerminalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ShiftNo = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    OpenedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OpenedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OpeningCash = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClosedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ExpectedCash = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ActualCash = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Variance = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
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
                    table.PrimaryKey("PK_PosShifts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PosShifts_PosTerminals_TerminalId",
                        column: x => x.TerminalId,
                        principalSchema: "pos",
                        principalTable: "PosTerminals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PosShifts_Users_ClosedByUserId",
                        column: x => x.ClosedByUserId,
                        principalSchema: "identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PosShifts_Users_OpenedByUserId",
                        column: x => x.OpenedByUserId,
                        principalSchema: "identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Vehicles",
                schema: "operations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    VehicleTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Make = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Model = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_Vehicles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Vehicles_VehicleTypes_VehicleTypeId",
                        column: x => x.VehicleTypeId,
                        principalSchema: "operations",
                        principalTable: "VehicleTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PosSales",
                schema: "pos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SaleNo = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ShiftId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TerminalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PartyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SaleDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SubTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GrandTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SalesOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeliveryOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SalesInvoiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PosShiftId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PosSales", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PosSales_DeliveryOrders_DeliveryOrderId",
                        column: x => x.DeliveryOrderId,
                        principalSchema: "sales",
                        principalTable: "DeliveryOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PosSales_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalSchema: "documents",
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PosSales_Parties_PartyId",
                        column: x => x.PartyId,
                        principalSchema: "procurement",
                        principalTable: "Parties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PosSales_PosShifts_PosShiftId",
                        column: x => x.PosShiftId,
                        principalSchema: "pos",
                        principalTable: "PosShifts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PosSales_PosShifts_ShiftId",
                        column: x => x.ShiftId,
                        principalSchema: "pos",
                        principalTable: "PosShifts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PosSales_PosTerminals_TerminalId",
                        column: x => x.TerminalId,
                        principalSchema: "pos",
                        principalTable: "PosTerminals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PosSales_SalesInvoices_SalesInvoiceId",
                        column: x => x.SalesInvoiceId,
                        principalSchema: "sales",
                        principalTable: "SalesInvoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PosSales_SalesOrders_SalesOrderId",
                        column: x => x.SalesOrderId,
                        principalSchema: "sales",
                        principalTable: "SalesOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VehicleAssignments",
                schema: "operations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DriverId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FromDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ToDate = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    table.PrimaryKey("PK_VehicleAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VehicleAssignments_Drivers_DriverId",
                        column: x => x.DriverId,
                        principalSchema: "operations",
                        principalTable: "Drivers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VehicleAssignments_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalSchema: "operations",
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PosPayments",
                schema: "pos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PosSaleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Tender = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CashBankAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CustomerPaymentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PosPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PosPayments_CashBankAccounts_CashBankAccountId",
                        column: x => x.CashBankAccountId,
                        principalSchema: "finance",
                        principalTable: "CashBankAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PosPayments_CustomerPayments_CustomerPaymentId",
                        column: x => x.CustomerPaymentId,
                        principalSchema: "sales",
                        principalTable: "CustomerPayments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PosPayments_PosSales_PosSaleId",
                        column: x => x.PosSaleId,
                        principalSchema: "pos",
                        principalTable: "PosSales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PosSaleLines",
                schema: "pos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PosSaleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LineNo = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Qty = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Rate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PosSaleLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PosSaleLines_PosSales_PosSaleId",
                        column: x => x.PosSaleId,
                        principalSchema: "pos",
                        principalTable: "PosSales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PosSaleLines_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "inventory",
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GatePasses",
                schema: "operations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    GatePassNo = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Direction = table.Column<int>(type: "int", nullable: false),
                    PassDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DriverId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SourceDocType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SourceEntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    WeighmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_GatePasses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GatePasses_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalSchema: "documents",
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GatePasses_Drivers_DriverId",
                        column: x => x.DriverId,
                        principalSchema: "operations",
                        principalTable: "Drivers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GatePasses_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalSchema: "operations",
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GatePassLines",
                schema: "operations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GatePassId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Qty = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Narration = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GatePassLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GatePassLines_GatePasses_GatePassId",
                        column: x => x.GatePassId,
                        principalSchema: "operations",
                        principalTable: "GatePasses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GatePassLines_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "inventory",
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Weighments",
                schema: "weighbridge",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TicketNo = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ScaleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VehicleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VehicleNo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PartyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Direction = table.Column<int>(type: "int", nullable: false),
                    FirstWeight = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FirstWeightAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SecondWeight = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    SecondWeightAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NetWeight = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    LinkedDocumentType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LinkedDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    GatePassId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_Weighments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Weighments_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalSchema: "documents",
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Weighments_GatePasses_GatePassId",
                        column: x => x.GatePassId,
                        principalSchema: "operations",
                        principalTable: "GatePasses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Weighments_Parties_PartyId",
                        column: x => x.PartyId,
                        principalSchema: "procurement",
                        principalTable: "Parties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Weighments_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "inventory",
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Weighments_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalSchema: "operations",
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Weighments_WeighbridgeScales_ScaleId",
                        column: x => x.ScaleId,
                        principalSchema: "weighbridge",
                        principalTable: "WeighbridgeScales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Drivers_EmployeeId",
                schema: "operations",
                table: "Drivers",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Drivers_TenantId_Code",
                schema: "operations",
                table: "Drivers",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GatePasses_DocumentId",
                schema: "operations",
                table: "GatePasses",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_GatePasses_DriverId",
                schema: "operations",
                table: "GatePasses",
                column: "DriverId");

            migrationBuilder.CreateIndex(
                name: "IX_GatePasses_TenantId_GatePassNo",
                schema: "operations",
                table: "GatePasses",
                columns: new[] { "TenantId", "GatePassNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GatePasses_VehicleId",
                schema: "operations",
                table: "GatePasses",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_GatePasses_WeighmentId",
                schema: "operations",
                table: "GatePasses",
                column: "WeighmentId");

            migrationBuilder.CreateIndex(
                name: "IX_GatePassLines_GatePassId",
                schema: "operations",
                table: "GatePassLines",
                column: "GatePassId");

            migrationBuilder.CreateIndex(
                name: "IX_GatePassLines_ProductId",
                schema: "operations",
                table: "GatePassLines",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_PosPayments_CashBankAccountId",
                schema: "pos",
                table: "PosPayments",
                column: "CashBankAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_PosPayments_CustomerPaymentId",
                schema: "pos",
                table: "PosPayments",
                column: "CustomerPaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_PosPayments_PosSaleId",
                schema: "pos",
                table: "PosPayments",
                column: "PosSaleId");

            migrationBuilder.CreateIndex(
                name: "IX_PosSaleLines_PosSaleId",
                schema: "pos",
                table: "PosSaleLines",
                column: "PosSaleId");

            migrationBuilder.CreateIndex(
                name: "IX_PosSaleLines_ProductId",
                schema: "pos",
                table: "PosSaleLines",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_PosSales_DeliveryOrderId",
                schema: "pos",
                table: "PosSales",
                column: "DeliveryOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_PosSales_DocumentId",
                schema: "pos",
                table: "PosSales",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_PosSales_PartyId",
                schema: "pos",
                table: "PosSales",
                column: "PartyId");

            migrationBuilder.CreateIndex(
                name: "IX_PosSales_PosShiftId",
                schema: "pos",
                table: "PosSales",
                column: "PosShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_PosSales_SalesInvoiceId",
                schema: "pos",
                table: "PosSales",
                column: "SalesInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_PosSales_SalesOrderId",
                schema: "pos",
                table: "PosSales",
                column: "SalesOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_PosSales_ShiftId",
                schema: "pos",
                table: "PosSales",
                column: "ShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_PosSales_TenantId_SaleNo",
                schema: "pos",
                table: "PosSales",
                columns: new[] { "TenantId", "SaleNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PosSales_TerminalId",
                schema: "pos",
                table: "PosSales",
                column: "TerminalId");

            migrationBuilder.CreateIndex(
                name: "IX_PosShifts_ClosedByUserId",
                schema: "pos",
                table: "PosShifts",
                column: "ClosedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PosShifts_OpenedByUserId",
                schema: "pos",
                table: "PosShifts",
                column: "OpenedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PosShifts_TenantId_ShiftNo",
                schema: "pos",
                table: "PosShifts",
                columns: new[] { "TenantId", "ShiftNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PosShifts_TerminalId",
                schema: "pos",
                table: "PosShifts",
                column: "TerminalId");

            migrationBuilder.CreateIndex(
                name: "IX_PosTerminals_BranchId",
                schema: "pos",
                table: "PosTerminals",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_PosTerminals_TenantId_Code",
                schema: "pos",
                table: "PosTerminals",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PosTerminals_WarehouseId",
                schema: "pos",
                table: "PosTerminals",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleAssignments_DriverId",
                schema: "operations",
                table: "VehicleAssignments",
                column: "DriverId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleAssignments_VehicleId",
                schema: "operations",
                table: "VehicleAssignments",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_TenantId_Code",
                schema: "operations",
                table: "Vehicles",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_VehicleTypeId",
                schema: "operations",
                table: "Vehicles",
                column: "VehicleTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleTypes_TenantId_Code",
                schema: "operations",
                table: "VehicleTypes",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WeighbridgeScales_TenantId_Code",
                schema: "weighbridge",
                table: "WeighbridgeScales",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Weighments_DocumentId",
                schema: "weighbridge",
                table: "Weighments",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_Weighments_GatePassId",
                schema: "weighbridge",
                table: "Weighments",
                column: "GatePassId");

            migrationBuilder.CreateIndex(
                name: "IX_Weighments_PartyId",
                schema: "weighbridge",
                table: "Weighments",
                column: "PartyId");

            migrationBuilder.CreateIndex(
                name: "IX_Weighments_ProductId",
                schema: "weighbridge",
                table: "Weighments",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Weighments_ScaleId",
                schema: "weighbridge",
                table: "Weighments",
                column: "ScaleId");

            migrationBuilder.CreateIndex(
                name: "IX_Weighments_TenantId_TicketNo",
                schema: "weighbridge",
                table: "Weighments",
                columns: new[] { "TenantId", "TicketNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Weighments_VehicleId",
                schema: "weighbridge",
                table: "Weighments",
                column: "VehicleId");

            migrationBuilder.AddForeignKey(
                name: "FK_GatePasses_Weighments_WeighmentId",
                schema: "operations",
                table: "GatePasses",
                column: "WeighmentId",
                principalSchema: "weighbridge",
                principalTable: "Weighments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GatePasses_Drivers_DriverId",
                schema: "operations",
                table: "GatePasses");

            migrationBuilder.DropForeignKey(
                name: "FK_GatePasses_Vehicles_VehicleId",
                schema: "operations",
                table: "GatePasses");

            migrationBuilder.DropForeignKey(
                name: "FK_Weighments_Vehicles_VehicleId",
                schema: "weighbridge",
                table: "Weighments");

            migrationBuilder.DropForeignKey(
                name: "FK_GatePasses_Weighments_WeighmentId",
                schema: "operations",
                table: "GatePasses");

            migrationBuilder.DropTable(
                name: "GatePassLines",
                schema: "operations");

            migrationBuilder.DropTable(
                name: "PosPayments",
                schema: "pos");

            migrationBuilder.DropTable(
                name: "PosSaleLines",
                schema: "pos");

            migrationBuilder.DropTable(
                name: "VehicleAssignments",
                schema: "operations");

            migrationBuilder.DropTable(
                name: "PosSales",
                schema: "pos");

            migrationBuilder.DropTable(
                name: "PosShifts",
                schema: "pos");

            migrationBuilder.DropTable(
                name: "PosTerminals",
                schema: "pos");

            migrationBuilder.DropTable(
                name: "Drivers",
                schema: "operations");

            migrationBuilder.DropTable(
                name: "Vehicles",
                schema: "operations");

            migrationBuilder.DropTable(
                name: "VehicleTypes",
                schema: "operations");

            migrationBuilder.DropTable(
                name: "Weighments",
                schema: "weighbridge");

            migrationBuilder.DropTable(
                name: "GatePasses",
                schema: "operations");

            migrationBuilder.DropTable(
                name: "WeighbridgeScales",
                schema: "weighbridge");
        }
    }
}
