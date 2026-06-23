using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ClassmateApii.Migrations
{ public partial class InitialCreate : Migration

    {

        /// <inheritdoc />

        protected override void Up(MigrationBuilder migrationBuilder)

        {

            migrationBuilder.CreateTable(

                name: "Users",

                columns: table => new

                {

                    Id = table.Column<int>(type: "integer", nullable: false)

                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),

                    GoogleId            = table.Column<string>(type: "character varying(128)",  maxLength: 128,  nullable: false),

                    Email               = table.Column<string>(type: "character varying(256)",  maxLength: 256,  nullable: false),

                    DisplayName         = table.Column<string>(type: "character varying(256)",  maxLength: 256,  nullable: false),

                    AvatarUrl           = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),

                    GoogleRefreshToken  = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),

                    FreeUsagesRemaining = table.Column<int>(type: "integer", nullable: false, defaultValue: 5),

                    EncryptedOpenAiKey  = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),

                    EncryptedGeminiKey  = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),

                    NotificationEmail   = table.Column<string>(type: "character varying(256)",  maxLength: 256,  nullable: false),

                    CreatedAt           = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),

                    UpdatedAt           = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)

                },

                constraints: table =>

                {

                    table.PrimaryKey("PK_Users", x => x.Id);

                });



            migrationBuilder.CreateTable(

                name: "ClassroomSettings",

                columns: table => new

                {

                    Id           = table.Column<int>(type: "integer", nullable: false)

                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),

                    UserId       = table.Column<int>(type: "integer", nullable: false),

                    CourseId     = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),

                    CourseName   = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),

                    AutoSolve    = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),

                    DelayMinutes = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),

                    CreatedAt    = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),

                    UpdatedAt    = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)

                },

                constraints: table =>

                {

                    table.PrimaryKey("PK_ClassroomSettings", x => x.Id);

                    table.ForeignKey(

                        name:       "FK_ClassroomSettings_Users_UserId",

                        column:     x => x.UserId,

                        principalTable: "Users",

                        principalColumn: "Id",

                        onDelete:   ReferentialAction.Cascade);

                });



            migrationBuilder.CreateIndex(

                name:   "IX_Users_GoogleId",

                table:  "Users",

                column: "GoogleId",

                unique: true);



            migrationBuilder.CreateIndex(

                name:   "IX_Users_Email",

                table:  "Users",

                column: "Email",

                unique: true);



            migrationBuilder.CreateIndex(

                name:   "IX_ClassroomSettings_UserId_CourseId",

                table:  "ClassroomSettings",

                columns: new[] { "UserId", "CourseId" },

                unique: true);

        }



        /// <inheritdoc />

        protected override void Down(MigrationBuilder migrationBuilder)

        {

            migrationBuilder.DropTable(name: "ClassroomSettings");

            migrationBuilder.DropTable(name: "Users");

        }

    }
}
