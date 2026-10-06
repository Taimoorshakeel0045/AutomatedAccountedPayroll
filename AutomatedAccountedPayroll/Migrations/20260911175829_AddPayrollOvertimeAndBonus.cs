using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutomatedAccountedPayroll.Migrations
{
    /// <inheritdoc />
    public partial class AddPayrollOvertimeAndBonus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Bonus",
                table: "Payrolls",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Overtime",
                table: "Payrolls",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Bonus",
                table: "Payrolls");

            migrationBuilder.DropColumn(
                name: "Overtime",
                table: "Payrolls");
        }
    }
}
