using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TuTa.Wms.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskTypeField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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

            migrationBuilder.AddColumn<ushort>(
                name: "ArriveAddress",
                table: "MachinePoints",
                type: "smallint unsigned",
                nullable: false,
                defaultValue: (ushort)0);

            migrationBuilder.AddColumn<ushort>(
                name: "FullMaterialAddress",
                table: "MachinePoints",
                type: "smallint unsigned",
                nullable: false,
                defaultValue: (ushort)0);

            migrationBuilder.AddColumn<string>(
                name: "IpAddress",
                table: "MachinePoints",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<ushort>(
                name: "JigExtendAddress",
                table: "MachinePoints",
                type: "smallint unsigned",
                nullable: false,
                defaultValue: (ushort)0);

            migrationBuilder.AddColumn<int>(
                name: "Port",
                table: "MachinePoints",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte>(
                name: "SlaveId",
                table: "MachinePoints",
                type: "tinyint unsigned",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<ushort>(
                name: "TakeOutAddress",
                table: "MachinePoints",
                type: "smallint unsigned",
                nullable: false,
                defaultValue: (ushort)0);

            migrationBuilder.AddColumn<string>(
                name: "TaskType",
                table: "MachinePoints",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            // 将地址类型转换为任务类型
            migrationBuilder.Sql("UPDATE MachinePoints SET TaskType = CASE AddressType WHEN 'FullMaterial' THEN 'EmptyBox' WHEN 'JigExtend' THEN 'MaterialOut' ELSE TaskType END WHERE TaskType IS NULL OR TaskType = ''");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ArriveAddress",
                table: "MachinePoints");

            migrationBuilder.DropColumn(
                name: "FullMaterialAddress",
                table: "MachinePoints");

            migrationBuilder.DropColumn(
                name: "IpAddress",
                table: "MachinePoints");

            migrationBuilder.DropColumn(
                name: "JigExtendAddress",
                table: "MachinePoints");

            migrationBuilder.DropColumn(
                name: "Port",
                table: "MachinePoints");

            migrationBuilder.DropColumn(
                name: "SlaveId",
                table: "MachinePoints");

            migrationBuilder.DropColumn(
                name: "TakeOutAddress",
                table: "MachinePoints");

            migrationBuilder.DropColumn(
                name: "TaskType",
                table: "MachinePoints");

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
    }
}
