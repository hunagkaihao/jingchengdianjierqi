using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TuTa.Wms.Migrations
{
    /// <inheritdoc />
    public partial class AddCellIdToMachinePoint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CellId",
                table: "MachinePoints",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.CreateIndex(
                name: "IX_MachinePoints_CellId",
                table: "MachinePoints",
                column: "CellId");

            migrationBuilder.AddForeignKey(
                name: "FK_MachinePoints_Cells_CellId",
                table: "MachinePoints",
                column: "CellId",
                principalTable: "Cells",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MachinePoints_Cells_CellId",
                table: "MachinePoints");

            migrationBuilder.DropIndex(
                name: "IX_MachinePoints_CellId",
                table: "MachinePoints");

            migrationBuilder.DropColumn(
                name: "CellId",
                table: "MachinePoints");
        }
    }
}
