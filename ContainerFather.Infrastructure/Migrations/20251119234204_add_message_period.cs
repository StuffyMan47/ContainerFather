using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContainerFather.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class add_message_period : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "period_type",
                schema: "public",
                table: "broadcast_message",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "period_type",
                schema: "public",
                table: "broadcast_message");
        }
    }
}
