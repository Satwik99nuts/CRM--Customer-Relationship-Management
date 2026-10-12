using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcxiomCRM.Migrations
{
    /// <inheritdoc />
    public partial class AssignmentCompliance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                table: "Opportunities",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "LeadId",
                table: "Opportunities",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OwnerId",
                table: "Opportunities",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ConvertedCustomerId",
                table: "Leads",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ConvertedDate",
                table: "Leads",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LeadCode",
                table: "Leads",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OwnerId",
                table: "Leads",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AssignedTo",
                table: "FollowUps",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Customers",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CustomerCode",
                table: "Customers",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedDate",
                table: "Customers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OwnerId",
                table: "Customers",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "State",
                table: "Customers",
                type: "TEXT",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "IpAddress",
                table: "AuditLogs",
                type: "TEXT",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NewValue",
                table: "AuditLogs",
                type: "TEXT",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OldValue",
                table: "AuditLogs",
                type: "TEXT",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Result",
                table: "AuditLogs",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "Success");

            migrationBuilder.AddColumn<string>(
                name: "DisplayName",
                table: "AspNetUsers",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "AspNetUsers",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "AssignedTo",
                table: "Activities",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("UPDATE Customers SET CustomerCode = 'CUS-' || CustomerId WHERE CustomerCode = '';");
            migrationBuilder.Sql("UPDATE Customers SET CreatedBy = 'System' WHERE CreatedBy = '';");
            migrationBuilder.Sql("UPDATE Leads SET LeadCode = 'LEA-' || LeadId WHERE LeadCode = '';");
            migrationBuilder.Sql("UPDATE AspNetUsers SET DisplayName = UserName WHERE DisplayName = '';");

            migrationBuilder.CreateIndex(
                name: "IX_Opportunities_LeadId",
                table: "Opportunities",
                column: "LeadId");

            migrationBuilder.CreateIndex(
                name: "IX_Leads_ConvertedCustomerId",
                table: "Leads",
                column: "ConvertedCustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Leads_LeadCode",
                table: "Leads",
                column: "LeadCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Customers_CustomerCode",
                table: "Customers",
                column: "CustomerCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Customers_Email",
                table: "Customers",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Customers_Phone",
                table: "Customers",
                column: "Phone",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Leads_Customers_ConvertedCustomerId",
                table: "Leads",
                column: "ConvertedCustomerId",
                principalTable: "Customers",
                principalColumn: "CustomerId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Opportunities_Leads_LeadId",
                table: "Opportunities",
                column: "LeadId",
                principalTable: "Leads",
                principalColumn: "LeadId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Leads_Customers_ConvertedCustomerId",
                table: "Leads");

            migrationBuilder.DropForeignKey(
                name: "FK_Opportunities_Leads_LeadId",
                table: "Opportunities");

            migrationBuilder.DropIndex(
                name: "IX_Opportunities_LeadId",
                table: "Opportunities");

            migrationBuilder.DropIndex(
                name: "IX_Leads_ConvertedCustomerId",
                table: "Leads");

            migrationBuilder.DropIndex(
                name: "IX_Leads_LeadCode",
                table: "Leads");

            migrationBuilder.DropIndex(
                name: "IX_Customers_CustomerCode",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_Customers_Email",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_Customers_Phone",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "Opportunities");

            migrationBuilder.DropColumn(
                name: "LeadId",
                table: "Opportunities");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "Opportunities");

            migrationBuilder.DropColumn(
                name: "ConvertedCustomerId",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "ConvertedDate",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "LeadCode",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "AssignedTo",
                table: "FollowUps");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "CustomerCode",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "ModifiedDate",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "State",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "IpAddress",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "NewValue",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "OldValue",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "Result",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "DisplayName",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "AssignedTo",
                table: "Activities");
        }
    }
}
