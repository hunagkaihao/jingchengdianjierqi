using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TuTa.Wms.Migrations
{
    /// <inheritdoc />
    public partial class AddMachineProductionTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MachinePoints_Machines_MachineId",
                table: "MachinePoints");

            migrationBuilder.DropTable(
                name: "ShelfMaterialBindings");

            migrationBuilder.DropColumn(
                name: "CreationTime",
                table: "Machines");

            migrationBuilder.DropColumn(
                name: "CreatorId",
                table: "Machines");

            migrationBuilder.DropColumn(
                name: "LastModificationTime",
                table: "Machines");

            migrationBuilder.DropColumn(
                name: "LastModifierId",
                table: "Machines");

            migrationBuilder.AddColumn<int>(
                name: "DoorCount",
                table: "Machines",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MachineNumber",
                table: "Machines",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<Guid>(
                name: "MachineId",
                table: "MachinePoints",
                type: "char(36)",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                collation: "ascii_general_ci",
                oldClrType: typeof(Guid),
                oldType: "char(36)",
                oldNullable: true)
                .OldAnnotation("Relational:Collation", "ascii_general_ci");

            migrationBuilder.AddColumn<string>(
                name: "AddressType",
                table: "MachinePoints",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ConcurrencyStamp",
                table: "MachinePoints",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "DoorNumber",
                table: "MachinePoints",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ExtraProperties",
                table: "MachinePoints",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "MachineProductions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    MachineId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    MaterialCode = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MaterialName = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ProductionCount = table.Column<int>(type: "int", nullable: false),
                    ProductionStatus = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    StartTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    EndTime = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ExtraProperties = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ConcurrencyStamp = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MachineProductions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MachineProductions_Machines_MachineId",
                        column: x => x.MachineId,
                        principalTable: "Machines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_MachineProductions_MachineId",
                table: "MachineProductions",
                column: "MachineId");

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
            migrationBuilder.DropForeignKey(
                name: "FK_MachinePoints_Machines_MachineId",
                table: "MachinePoints");

            migrationBuilder.DropTable(
                name: "MachineProductions");

            migrationBuilder.DropColumn(
                name: "DoorCount",
                table: "Machines");

            migrationBuilder.DropColumn(
                name: "MachineNumber",
                table: "Machines");

            migrationBuilder.DropColumn(
                name: "AddressType",
                table: "MachinePoints");

            migrationBuilder.DropColumn(
                name: "ConcurrencyStamp",
                table: "MachinePoints");

            migrationBuilder.DropColumn(
                name: "DoorNumber",
                table: "MachinePoints");

            migrationBuilder.DropColumn(
                name: "ExtraProperties",
                table: "MachinePoints");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreationTime",
                table: "Machines",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "CreatorId",
                table: "Machines",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastModificationTime",
                table: "Machines",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LastModifierId",
                table: "Machines",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AlterColumn<Guid>(
                name: "MachineId",
                table: "MachinePoints",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci",
                oldClrType: typeof(Guid),
                oldType: "char(36)")
                .OldAnnotation("Relational:Collation", "ascii_general_ci");

            migrationBuilder.CreateTable(
                name: "ShelfMaterialBindings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    MaterialCode = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MaterialName = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ShelfName = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    WarehouseAreaId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShelfMaterialBindings", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ShelfMaterialBindings_ShelfName_WarehouseAreaId",
                table: "ShelfMaterialBindings",
                columns: new[] { "ShelfName", "WarehouseAreaId" });

            migrationBuilder.AddForeignKey(
                name: "FK_MachinePoints_Machines_MachineId",
                table: "MachinePoints",
                column: "MachineId",
                principalTable: "Machines",
                principalColumn: "Id");
        }
    }
}
