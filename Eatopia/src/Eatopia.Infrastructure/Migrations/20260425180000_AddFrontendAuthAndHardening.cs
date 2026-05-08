using System;
using Eatopia.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Eatopia.Infrastructure.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(EatopiaDbContext))]
    [Migration("20260425180000_AddFrontendAuthAndHardening")]
    public partial class AddFrontendAuthAndHardening : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Make every FK pointing to Users use NoAction to avoid cascade-path issues.
            migrationBuilder.DropForeignKey("FK_ChatMessages_Users_SenderId", "ChatMessages");
            migrationBuilder.DropForeignKey("FK_ChatParticipants_Users_UserId", "ChatParticipants");
            migrationBuilder.DropForeignKey("FK_MealLogs_Users_UserId", "MealLogs");
            migrationBuilder.DropForeignKey("FK_Medications_Users_UserId", "Medications");
            migrationBuilder.DropForeignKey("FK_Recipes_Users_AuthorId", "Recipes");
            migrationBuilder.DropForeignKey("FK_UserAllergies_Users_UserId", "UserAllergies");
            migrationBuilder.DropForeignKey("FK_UserDislikedFoods_Users_UserId", "UserDislikedFoods");
            migrationBuilder.DropForeignKey("FK_WaterGoals_Users_UserId", "WaterGoals");
            migrationBuilder.DropForeignKey("FK_WaterLogs_Users_UserId", "WaterLogs");
            migrationBuilder.DropForeignKey("FK_RecipeSaved_Users_UserId", "RecipeSaved");

            migrationBuilder.AlterColumn<string>(
                name: "Role",
                table: "Users",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "User",
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldDefaultValue: "user");

            migrationBuilder.AlterColumn<string>(
                name: "Gender",
                table: "Users",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ActivityLevel",
                table: "Users",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Goal",
                table: "Users",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "BirthDate",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "Users",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "Users",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProfileImageUrl",
                table: "Users",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Username",
                table: "Users",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true,
                filter: "[Username] IS NOT NULL");

            migrationBuilder.AddForeignKey("FK_ChatMessages_Users_SenderId", "ChatMessages", "SenderId", "Users", principalColumn: "Id");
            migrationBuilder.AddForeignKey("FK_ChatParticipants_Users_UserId", "ChatParticipants", "UserId", "Users", principalColumn: "Id");
            migrationBuilder.AddForeignKey("FK_MealLogs_Users_UserId", "MealLogs", "UserId", "Users", principalColumn: "Id");
            migrationBuilder.AddForeignKey("FK_Medications_Users_UserId", "Medications", "UserId", "Users", principalColumn: "Id");
            migrationBuilder.AddForeignKey("FK_Recipes_Users_AuthorId", "Recipes", "AuthorId", "Users", principalColumn: "Id");
            migrationBuilder.AddForeignKey("FK_UserAllergies_Users_UserId", "UserAllergies", "UserId", "Users", principalColumn: "Id");
            migrationBuilder.AddForeignKey("FK_UserDislikedFoods_Users_UserId", "UserDislikedFoods", "UserId", "Users", principalColumn: "Id");
            migrationBuilder.AddForeignKey("FK_WaterGoals_Users_UserId", "WaterGoals", "UserId", "Users", principalColumn: "Id");
            migrationBuilder.AddForeignKey("FK_WaterLogs_Users_UserId", "WaterLogs", "UserId", "Users", principalColumn: "Id");
            migrationBuilder.AddForeignKey("FK_RecipeSaved_Users_UserId", "RecipeSaved", "UserId", "Users", principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey("FK_ChatMessages_Users_SenderId", "ChatMessages");
            migrationBuilder.DropForeignKey("FK_ChatParticipants_Users_UserId", "ChatParticipants");
            migrationBuilder.DropForeignKey("FK_MealLogs_Users_UserId", "MealLogs");
            migrationBuilder.DropForeignKey("FK_Medications_Users_UserId", "Medications");
            migrationBuilder.DropForeignKey("FK_Recipes_Users_AuthorId", "Recipes");
            migrationBuilder.DropForeignKey("FK_UserAllergies_Users_UserId", "UserAllergies");
            migrationBuilder.DropForeignKey("FK_UserDislikedFoods_Users_UserId", "UserDislikedFoods");
            migrationBuilder.DropForeignKey("FK_WaterGoals_Users_UserId", "WaterGoals");
            migrationBuilder.DropForeignKey("FK_WaterLogs_Users_UserId", "WaterLogs");
            migrationBuilder.DropForeignKey("FK_RecipeSaved_Users_UserId", "RecipeSaved");

            migrationBuilder.DropIndex("IX_Users_Username", "Users");

            migrationBuilder.DropColumn("BirthDate", "Users");
            migrationBuilder.DropColumn("Location", "Users");
            migrationBuilder.DropColumn("Phone", "Users");
            migrationBuilder.DropColumn("ProfileImageUrl", "Users");
            migrationBuilder.DropColumn("Username", "Users");

            migrationBuilder.AlterColumn<string>(
                name: "Role",
                table: "Users",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "user",
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldDefaultValue: "User");

            migrationBuilder.AlterColumn<string>(
                name: "Gender",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ActivityLevel",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Goal",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AddForeignKey("FK_ChatMessages_Users_SenderId", "ChatMessages", "SenderId", "Users", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
            migrationBuilder.AddForeignKey("FK_ChatParticipants_Users_UserId", "ChatParticipants", "UserId", "Users", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
            migrationBuilder.AddForeignKey("FK_MealLogs_Users_UserId", "MealLogs", "UserId", "Users", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
            migrationBuilder.AddForeignKey("FK_Medications_Users_UserId", "Medications", "UserId", "Users", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
            migrationBuilder.AddForeignKey("FK_Recipes_Users_AuthorId", "Recipes", "AuthorId", "Users", principalColumn: "Id", onDelete: ReferentialAction.SetNull);
            migrationBuilder.AddForeignKey("FK_UserAllergies_Users_UserId", "UserAllergies", "UserId", "Users", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
            migrationBuilder.AddForeignKey("FK_UserDislikedFoods_Users_UserId", "UserDislikedFoods", "UserId", "Users", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
            migrationBuilder.AddForeignKey("FK_WaterGoals_Users_UserId", "WaterGoals", "UserId", "Users", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
            migrationBuilder.AddForeignKey("FK_WaterLogs_Users_UserId", "WaterLogs", "UserId", "Users", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
            migrationBuilder.AddForeignKey("FK_RecipeSaved_Users_UserId", "RecipeSaved", "UserId", "Users", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
        }
    }
}
