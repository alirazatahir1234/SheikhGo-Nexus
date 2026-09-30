using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SheikhGoNexusERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAiPhase10C : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AgentRunId",
                schema: "ai",
                table: "PendingActions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AgentStepId",
                schema: "ai",
                table: "PendingActions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AgentRuns",
                schema: "ai",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FinancialYearId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConversationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AgentType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Goal = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PlanJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FailureReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AgentRuns_Conversations_ConversationId",
                        column: x => x.ConversationId,
                        principalSchema: "ai",
                        principalTable: "Conversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AgentSteps",
                schema: "ai",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AgentRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StepNo = table.Column<int>(type: "int", nullable: false),
                    ToolName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    InputJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OutputJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RequiresConfirmation = table.Column<bool>(type: "bit", nullable: false),
                    LinkedPendingActionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ExecutedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AgentSteps_AgentRuns_AgentRunId",
                        column: x => x.AgentRunId,
                        principalSchema: "ai",
                        principalTable: "AgentRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PendingActions_AgentRunId",
                schema: "ai",
                table: "PendingActions",
                column: "AgentRunId");

            migrationBuilder.CreateIndex(
                name: "IX_PendingActions_AgentStepId",
                schema: "ai",
                table: "PendingActions",
                column: "AgentStepId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentRuns_ConversationId",
                schema: "ai",
                table: "AgentRuns",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentRuns_TenantId_UserId_Status",
                schema: "ai",
                table: "AgentRuns",
                columns: new[] { "TenantId", "UserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_AgentSteps_AgentRunId_StepNo",
                schema: "ai",
                table: "AgentSteps",
                columns: new[] { "AgentRunId", "StepNo" });

            migrationBuilder.AddForeignKey(
                name: "FK_PendingActions_AgentRuns_AgentRunId",
                schema: "ai",
                table: "PendingActions",
                column: "AgentRunId",
                principalSchema: "ai",
                principalTable: "AgentRuns",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PendingActions_AgentSteps_AgentStepId",
                schema: "ai",
                table: "PendingActions",
                column: "AgentStepId",
                principalSchema: "ai",
                principalTable: "AgentSteps",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PendingActions_AgentRuns_AgentRunId",
                schema: "ai",
                table: "PendingActions");

            migrationBuilder.DropForeignKey(
                name: "FK_PendingActions_AgentSteps_AgentStepId",
                schema: "ai",
                table: "PendingActions");

            migrationBuilder.DropTable(
                name: "AgentSteps",
                schema: "ai");

            migrationBuilder.DropTable(
                name: "AgentRuns",
                schema: "ai");

            migrationBuilder.DropIndex(
                name: "IX_PendingActions_AgentRunId",
                schema: "ai",
                table: "PendingActions");

            migrationBuilder.DropIndex(
                name: "IX_PendingActions_AgentStepId",
                schema: "ai",
                table: "PendingActions");

            migrationBuilder.DropColumn(
                name: "AgentRunId",
                schema: "ai",
                table: "PendingActions");

            migrationBuilder.DropColumn(
                name: "AgentStepId",
                schema: "ai",
                table: "PendingActions");
        }
    }
}
