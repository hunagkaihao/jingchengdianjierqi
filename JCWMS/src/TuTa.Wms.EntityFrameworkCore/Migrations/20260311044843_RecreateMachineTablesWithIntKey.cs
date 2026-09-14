using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TuTa.Wms.Migrations
{
    /// <inheritdoc />
    public partial class RecreateMachineTablesWithIntKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 先删除外键约束
            migrationBuilder.DropForeignKey(
                name: "FK_MachineProductions_Machines_MachineId",
                table: "MachineProductions");

            migrationBuilder.DropForeignKey(
                name: "FK_MachinePoints_Machines_MachineId",
                table: "MachinePoints");

            // 修改MachinePoints表
            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "MachinePoints",
                type: "int",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "char(36)")
                .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn)
                .OldAnnotation("Relational:Collation", "ascii_general_ci");

            migrationBuilder.AlterColumn<int>(
                name: "MachineId",
                table: "MachinePoints",
                type: "int",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "char(36)")
                .OldAnnotation("Relational:Collation", "ascii_general_ci");

            // 修改MachineProductions表
            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "MachineProductions",
                type: "int",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "char(36)")
                .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn)
                .OldAnnotation("Relational:Collation", "ascii_general_ci");

            migrationBuilder.AlterColumn<int>(
                name: "MachineId",
                table: "MachineProductions",
                type: "int",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "char(36)")
                .OldAnnotation("Relational:Collation", "ascii_general_ci");

            // 修改Machines表
            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Machines",
                type: "int",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "char(36)")
                .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn)
                .OldAnnotation("Relational:Collation", "ascii_general_ci");

            // 重新添加外键约束
            migrationBuilder.AddForeignKey(
                name: "FK_MachineProductions_Machines_MachineId",
                table: "MachineProductions",
                column: "MachineId",
                principalTable: "Machines",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MachinePoints_Machines_MachineId",
                table: "MachinePoints",
                column: "MachineId",
                principalTable: "Machines",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 先删除外键约束
            migrationBuilder.DropForeignKey(
                name: "FK_MachineProductions_Machines_MachineId",
                table: "MachineProductions");

            migrationBuilder.DropForeignKey(
                name: "FK_MachinePoints_Machines_MachineId",
                table: "MachinePoints");

            // 修改Machines表
            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "Machines",
                type: "char(36)",
                nullable: false,
                collation: "ascii_general_ci",
                oldClrType: typeof(int),
                oldType: "int")
                .OldAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            // 修改MachineProductions表
            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "MachineProductions",
                type: "char(36)",
                nullable: false,
                collation: "ascii_general_ci",
                oldClrType: typeof(int),
                oldType: "int")
                .OldAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<Guid>(
                name: "MachineId",
                table: "MachineProductions",
                type: "char(36)",
                nullable: false,
                collation: "ascii_general_ci",
                oldClrType: typeof(int),
                oldType: "int");

            // 修改MachinePoints表
            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "MachinePoints",
                type: "char(36)",
                nullable: false,
                collation: "ascii_general_ci",
                oldClrType: typeof(int),
                oldType: "int")
                .OldAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AlterColumn<Guid>(
                name: "MachineId",
                table: "MachinePoints",
                type: "char(36)",
                nullable: false,
                collation: "ascii_general_ci",
                oldClrType: typeof(int),
                oldType: "int");

            // 重新添加外键约束
            migrationBuilder.AddForeignKey(
                name: "FK_MachineProductions_Machines_MachineId",
                table: "MachineProductions",
                column: "MachineId",
                principalTable: "Machines",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MachinePoints_Machines_MachineId",
                table: "MachinePoints",
                column: "MachineId",
                principalTable: "Machines",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
