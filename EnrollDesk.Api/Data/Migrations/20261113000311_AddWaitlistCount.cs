using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnrollDesk.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddWaitlistCount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "WaitlistCount",
                table: "Sections",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WaitlistCount",
                table: "Sections");
        }
    }
}
