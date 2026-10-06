using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutomatedAccountedPayroll.Migrations
{
    /// <inheritdoc />
    public partial class AddLoanDeductionToPayroll : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "LoanDeduction",
                table: "Payrolls",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LoanDeduction",
                table: "Payrolls");
        }
    }
}
