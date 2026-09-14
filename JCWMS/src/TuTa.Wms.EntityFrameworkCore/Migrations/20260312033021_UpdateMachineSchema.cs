using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TuTa.Wms.Migrations
{
    /// <inheritdoc />
    public partial class UpdateMachineSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Port",
                table: "Machines",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte>(
                name: "SlaveId",
                table: "Machines",
                type: "tinyint unsigned",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AlterColumn<ushort>(
                name: "Address",
                table: "MachinePoints",
                type: "smallint unsigned",
                nullable: false,
                defaultValue: (ushort)0,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "CellCode",
                table: "MachinePoints",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Port",
                table: "Machines");

            migrationBuilder.DropColumn(
                name: "SlaveId",
                table: "Machines");

            migrationBuilder.DropColumn(
                name: "CellCode",
                table: "MachinePoints");

            migrationBuilder.AlterColumn<string>(
                name: "Address",
                table: "MachinePoints",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(ushort),
                oldType: "smallint unsigned")
                .Annotation("MySql:CharSet", "utf8mb4");
        }
    }
}
