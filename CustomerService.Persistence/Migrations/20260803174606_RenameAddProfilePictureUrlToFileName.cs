using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CustomerService.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenameAddProfilePictureUrlToFileName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ProfilePictureUrl",
                table: "Customers",
                newName: "ProfilePictureFileName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ProfilePictureFileName",
                table: "Customers",
                newName: "ProfilePictureUrl");
        }
    }
}
