using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContainerFather.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class chat_update : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "telegram_id",
                schema: "public",
                table: "chat",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "telegram_id",
                schema: "public",
                table: "chat");
        }
    }
}
