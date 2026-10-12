using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcxiomCRM.Migrations
{
    /// <inheritdoc />
    public partial class FollowUpOpportunity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OpportunityId",
                table: "FollowUps",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_FollowUps_OpportunityId",
                table: "FollowUps",
                column: "OpportunityId");

            migrationBuilder.AddForeignKey(
                name: "FK_FollowUps_Opportunities_OpportunityId",
                table: "FollowUps",
                column: "OpportunityId",
                principalTable: "Opportunities",
                principalColumn: "OpportunityId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FollowUps_Opportunities_OpportunityId",
                table: "FollowUps");

            migrationBuilder.DropIndex(
                name: "IX_FollowUps_OpportunityId",
                table: "FollowUps");

            migrationBuilder.DropColumn(
                name: "OpportunityId",
                table: "FollowUps");
        }
    }
}
