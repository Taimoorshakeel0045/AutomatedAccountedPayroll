using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutomatedAccountedPayroll.Migrations
{
    /// <inheritdoc />
    public partial class AddAdvanceSalaryToPayroll : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AdvanceSalary",
                table: "Payrolls",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdvanceSalary",
                table: "Payrolls");
        }
    }
}
