using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContainerFather.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class update_message_url : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_container_message_message_id",
                schema: "public",
                table: "container");

            migrationBuilder.DropIndex(
                name: "IX_container_message_id",
                schema: "public",
                table: "container");

            migrationBuilder.AlterColumn<string>(
                name: "message_id",
                schema: "public",
                table: "container",
                type: "text",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<long>(
                name: "message_id",
                schema: "public",
                table: "container",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_container_message_id",
                schema: "public",
                table: "container",
                column: "message_id");

            migrationBuilder.AddForeignKey(
                name: "FK_container_message_message_id",
                schema: "public",
                table: "container",
                column: "message_id",
                principalSchema: "public",
                principalTable: "message",
                principalColumn: "id");
        }
    }
}
