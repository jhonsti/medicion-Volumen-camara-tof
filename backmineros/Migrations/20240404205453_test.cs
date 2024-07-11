using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BackMineros.Migrations
{
    /// <inheritdoc />
    public partial class test : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ControlCamara",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    zmin = table.Column<int>(type: "int", nullable: false),
                    zmax = table.Column<int>(type: "int", nullable: false),
                    xmin = table.Column<int>(type: "int", nullable: false),
                    xmax = table.Column<int>(type: "int", nullable: false),
                    ymin = table.Column<int>(type: "int", nullable: false),
                    ymax = table.Column<int>(type: "int", nullable: false),
                    estado = table.Column<bool>(type: "bit", nullable: false),
                    actualizarImagenReferencia = table.Column<bool>(type: "bit", nullable: false),
                    pathImgReferencia = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    pathExe = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ControlCamara", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Mediciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Volumen = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Turno = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Mediciones", x => x.Id);
                    table.CheckConstraint("CK_Turno", "Turno IN ('Mañana', 'Tarde', 'Noche')");
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ControlCamara");

            migrationBuilder.DropTable(
                name: "Mediciones");
        }
    }
}
