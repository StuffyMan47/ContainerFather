using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContainerFather.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class uniq_article : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "article_id",
                schema: "public",
                table: "container",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "article_id",
                schema: "public",
                table: "container");
        }
    }
}
