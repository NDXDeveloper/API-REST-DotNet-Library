using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace src.Migrations
{
    public partial class AddViewCountToBooksMagazines : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Ajouter les nouvelles colonnes ViewCount et DownloadCount à la table BooksMagazines
            migrationBuilder.AddColumn<int>(
                name: "ViewCount",
                table: "BooksMagazines",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DownloadCount",
                table: "BooksMagazines",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Supprimer les colonnes ViewCount et DownloadCount en cas de rollback
            migrationBuilder.DropColumn(
                name: "ViewCount",
                table: "BooksMagazines");

            migrationBuilder.DropColumn(
                name: "DownloadCount",
                table: "BooksMagazines");
        }
    }
}
