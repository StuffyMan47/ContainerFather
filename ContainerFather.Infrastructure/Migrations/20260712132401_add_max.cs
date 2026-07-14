using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContainerFather.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class add_max : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "telegram_id",
                schema: "public",
                table: "chat",
                newName: "chat_id");

            migrationBuilder.AddColumn<int>(
                name: "messenger_type",
                schema: "public",
                table: "user",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "messenger_type",
                schema: "public",
                table: "chat",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "messenger_type",
                schema: "public",
                table: "user");

            migrationBuilder.DropColumn(
                name: "messenger_type",
                schema: "public",
                table: "chat");

            migrationBuilder.RenameColumn(
                name: "chat_id",
                schema: "public",
                table: "chat",
                newName: "telegram_id");
        }
    }
}
