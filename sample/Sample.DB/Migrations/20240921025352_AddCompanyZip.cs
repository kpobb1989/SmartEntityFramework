using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sample.DB.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyZip : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Zip",
                table: "Companies",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Zip",
                table: "Companies");
        }
    }
}
