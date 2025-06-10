using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlataformaJiujitsu.Migrations
{
    /// <inheritdoc />
    public partial class FixRelacionamentoChaveInscricao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Chaves_Categoria_CategoriaId",
                table: "Chaves");

            migrationBuilder.DropForeignKey(
                name: "FK_Inscricoes_Chaves_ChaveId",
                table: "Inscricoes");

            migrationBuilder.DropIndex(
                name: "IX_Chaves_CategoriaId",
                table: "Chaves");

            migrationBuilder.DropColumn(
                name: "PlacarAtleta1",
                table: "Lutas");

            migrationBuilder.DropColumn(
                name: "PlacarAtleta2",
                table: "Lutas");

            migrationBuilder.AlterColumn<int>(
                name: "ChaveId",
                table: "Lutas",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_Inscricoes_Chaves_ChaveId",
                table: "Inscricoes",
                column: "ChaveId",
                principalTable: "Chaves",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Inscricoes_Chaves_ChaveId",
                table: "Inscricoes");

            migrationBuilder.AlterColumn<int>(
                name: "ChaveId",
                table: "Lutas",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PlacarAtleta1",
                table: "Lutas",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PlacarAtleta2",
                table: "Lutas",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Chaves_CategoriaId",
                table: "Chaves",
                column: "CategoriaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Chaves_Categoria_CategoriaId",
                table: "Chaves",
                column: "CategoriaId",
                principalTable: "Categoria",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Inscricoes_Chaves_ChaveId",
                table: "Inscricoes",
                column: "ChaveId",
                principalTable: "Chaves",
                principalColumn: "Id");
        }
    }
}
