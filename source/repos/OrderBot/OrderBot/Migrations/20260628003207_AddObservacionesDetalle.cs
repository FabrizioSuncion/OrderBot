using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderBot.Migrations
{
    /// <inheritdoc />
    public partial class AddObservacionesDetalle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Observaciones",
                table: "DetallesPedido",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Observaciones",
                table: "DetallesPedido");
        }
    }
}
