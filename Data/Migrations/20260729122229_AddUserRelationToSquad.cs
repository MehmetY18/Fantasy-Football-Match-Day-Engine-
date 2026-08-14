using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserRelationToSquad : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Squads_user_id",
                table: "Squads",
                column: "user_id");

            migrationBuilder.AddForeignKey(
                name: "FK_Squads_Users_user_id",
                table: "Squads",
                column: "user_id",
                principalTable: "Users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Squads_Users_user_id",
                table: "Squads");

            migrationBuilder.DropIndex(
                name: "IX_Squads_user_id",
                table: "Squads");
        }
    }
}
