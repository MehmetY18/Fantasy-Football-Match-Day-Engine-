using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Fixtures",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    match_id = table.Column<string>(type: "text", nullable: false),
                    minute = table.Column<int>(type: "integer", nullable: false),
                    type = table.Column<string>(type: "text", nullable: false),
                    player_id = table.Column<string>(type: "text", nullable: false),
                    assist_player_id = table.Column<string>(type: "text", nullable: false),
                    team_id = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fixtures", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Matches",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    home_team_id = table.Column<string>(type: "text", nullable: false),
                    away_team_id = table.Column<string>(type: "text", nullable: false),
                    kickoff = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    gameweek = table.Column<int>(type: "integer", nullable: false),
                    Score_home = table.Column<int>(type: "integer", nullable: false),
                    Score_away = table.Column<int>(type: "integer", nullable: false),
                    minute = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Matches", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Players",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    position = table.Column<string>(type: "text", nullable: false),
                    team_id = table.Column<string>(type: "text", nullable: false),
                    price = table.Column<decimal>(type: "numeric", nullable: false),
                    total_points = table.Column<int>(type: "integer", nullable: false),
                    stats_goals = table.Column<int>(type: "integer", nullable: false),
                    stats_assists = table.Column<int>(type: "integer", nullable: false),
                    stats_yellow_cards = table.Column<int>(type: "integer", nullable: false),
                    stats_red_cards = table.Column<int>(type: "integer", nullable: false),
                    stats_minutes_played = table.Column<int>(type: "integer", nullable: false),
                    stats_clean_sheets = table.Column<int>(type: "integer", nullable: false),
                    stats_own_goals = table.Column<int>(type: "integer", nullable: false),
                    stats_penalties_missed = table.Column<int>(type: "integer", nullable: false),
                    stats_saves = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Players", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Teams",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    Short = table.Column<string>(type: "text", nullable: false),
                    stadium = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Teams", x => x.id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Fixtures");

            migrationBuilder.DropTable(
                name: "Matches");

            migrationBuilder.DropTable(
                name: "Players");

            migrationBuilder.DropTable(
                name: "Teams");
        }
    }
}
