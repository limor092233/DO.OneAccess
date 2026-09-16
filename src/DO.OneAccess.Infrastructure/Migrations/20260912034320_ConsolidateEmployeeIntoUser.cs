using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DO.OneAccess.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ConsolidateEmployeeIntoUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Add new columns to Users table as nullable initially to allow data copying
            migrationBuilder.AddColumn<string>(
                name: "EmployeeNumber",
                table: "Users",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "Users",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MiddleName",
                table: "Users",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                table: "Users",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Users",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Position",
                table: "Users",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SectionId",
                table: "Users",
                type: "int",
                nullable: true);

            // 2. Data copy: copy existing employee profile data into Users
            migrationBuilder.Sql(@"
                UPDATE u
                SET u.EmployeeNumber = e.EmployeeNumber,
                    u.FirstName = e.FirstName,
                    u.MiddleName = e.MiddleName,
                    u.LastName = e.LastName,
                    u.Email = e.Email,
                    u.Position = e.Position,
                    u.SectionId = e.SectionId
                FROM Users u
                INNER JOIN Employees e ON u.EmployeeId = e.EmployeeId;
            ");

            // 3. Fail-fast validation: Ensure all users have valid mandatory profile fields
            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1 FROM Users
                    WHERE EmployeeNumber IS NULL OR LTRIM(RTRIM(EmployeeNumber)) = ''
                       OR FirstName IS NULL OR LTRIM(RTRIM(FirstName)) = ''
                       OR LastName IS NULL OR LTRIM(RTRIM(LastName)) = ''
                       OR Email IS NULL OR LTRIM(RTRIM(Email)) = ''
                )
                BEGIN
                    THROW 51000, 'Employee/User consolidation failed: one or more Users have missing mandatory profile data after copy from Employees.', 1;
                END
            ");

            // 4. Alter mandatory columns to NOT NULL (Position, MiddleName, and SectionId remain nullable)
            migrationBuilder.AlterColumn<string>(
                name: "EmployeeNumber",
                table: "Users",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FirstName",
                table: "Users",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "LastName",
                table: "Users",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Users",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            // 5. Indexes and foreign keys on Users
            migrationBuilder.CreateIndex(
                name: "IX_Users_EmployeeNumber",
                table: "Users",
                column: "EmployeeNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_SectionId",
                table: "Users",
                column: "SectionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Sections_SectionId",
                table: "Users",
                column: "SectionId",
                principalTable: "Sections",
                principalColumn: "SectionId",
                onDelete: ReferentialAction.Restrict);

            // 6. Drop obsolete Employee relationship and table
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Employees_EmployeeId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_EmployeeId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "EmployeeId",
                table: "Users");

            migrationBuilder.DropTable(
                name: "Employees");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Employees",
                columns: table => new
                {
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SectionId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EmployeeNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MiddleName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Position = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Employees", x => x.EmployeeId);
                    table.ForeignKey(
                        name: "FK_Employees_Sections_SectionId",
                        column: x => x.SectionId,
                        principalTable: "Sections",
                        principalColumn: "SectionId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddColumn<Guid>(
                name: "EmployeeId",
                table: "Users",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.Sql(@"
                INSERT INTO Employees (EmployeeId, EmployeeNumber, FirstName, MiddleName, LastName, Email, Position, SectionId, IsActive, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy)
                SELECT UserId, EmployeeNumber, FirstName, MiddleName, LastName, Email, ISNULL(Position, ''), SectionId, IsActive, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy
                FROM Users;

                UPDATE Users SET EmployeeId = UserId;
            ");

            migrationBuilder.AlterColumn<Guid>(
                name: "EmployeeId",
                table: "Users",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Sections_SectionId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_EmployeeNumber",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_SectionId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "EmployeeNumber",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "FirstName",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LastName",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "MiddleName",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Position",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SectionId",
                table: "Users");

            migrationBuilder.CreateIndex(
                name: "IX_Users_EmployeeId",
                table: "Users",
                column: "EmployeeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Employees_EmployeeNumber",
                table: "Employees",
                column: "EmployeeNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Employees_IsActive",
                table: "Employees",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_SectionId",
                table: "Employees",
                column: "SectionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Employees_EmployeeId",
                table: "Users",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "EmployeeId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
