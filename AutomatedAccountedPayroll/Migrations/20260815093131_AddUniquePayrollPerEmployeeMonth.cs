using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutomatedAccountedPayroll.Migrations
{
    /// <inheritdoc />
    public partial class AddUniquePayrollPerEmployeeMonth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Payrolls_EmployeeId",
                table: "Payrolls");

            migrationBuilder.CreateIndex(
                name: "IX_Payrolls_EmployeeId_PayrollMonth",
                table: "Payrolls",
                columns: new[] { "EmployeeId", "PayrollMonth" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Payrolls_EmployeeId_PayrollMonth",
                table: "Payrolls");

            migrationBuilder.CreateIndex(
                name: "IX_Payrolls_EmployeeId",
                table: "Payrolls",
                column: "EmployeeId");
        }
    }
}
