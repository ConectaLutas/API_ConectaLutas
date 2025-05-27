using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlataformaAPI.Data; // Certifique-se de que este namespace está correto
using PlataformaAPI.Models; // Certifique-se de que este namespace está correto
using Microsoft.AspNetCore.Authentication.JwtBearer; // Importante: adicione este using

namespace PlataformaJiujitsu.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InscricaoController : ControllerBase
    {
        private readonly UserManager<Usuario> _userManager;
        private readonly ApplicationDbContext _context;

        public InscricaoController(UserManager<Usuario> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        [HttpPost("{campeonatoId}/inscrever")]
        // **MUDANÇA AQUI:** Especifique o esquema de autenticação para JWT Bearer
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> InscreverNoCampeonato([FromRoute] int campeonatoId)
        {
            var usuarioAtual = await _userManager.GetUserAsync(User);

            if (usuarioAtual == null)
            {
                // Este caso já deveria ser tratado pelo [Authorize], mas é uma boa prática
                // ter uma verificação para usuários não autenticados ou tokens inválidos que
                // possam ter passado por alguma falha de configuração.
                return Unauthorized("Usuário não autenticado.");
            }

            if (usuarioAtual.TipoUsuario != TipoUsuario.Atleta)
                return Unauthorized("Somente atletas podem se inscrever.");

            var atleta = await _context.Atletas.FirstOrDefaultAsync(a => a.UsuarioId == usuarioAtual.Id);
            if (atleta == null)
                return Unauthorized("Atleta não encontrado."); // Não adicione atleta.Id em mensagens de erro para o front, pode ser um risco de segurança.

            var campeonato = await _context.Campeonatos
                .Include(c => c.Inscricoes)
                .FirstOrDefaultAsync(c => c.Id == campeonatoId);
            if (campeonato == null)
                return NotFound("Campeonato não encontrado.");

            if (campeonato.Status != StatusCampeonato.EmAndamento)
                return BadRequest("As inscrições para este campeonato estão encerradas."); // Remova atleta.Id aqui também

            if (campeonato.Inscricoes.Count >= campeonato.MaxInscritos)
                return BadRequest("Número máximo de inscritos alcançado.");

            bool inscrito = await _context.Inscricoes
                .AnyAsync(i => i.AtletaId == atleta.Id && i.CampeonatoId == campeonato.Id);
            if (inscrito)
                return BadRequest("O atleta já está inscrito neste campeonato."); // Remova atleta.Id aqui também

            // Calcular idade do atleta
            var idade = DateTime.Today.Year - atleta.DataNascimento.Year;
            if (atleta.DataNascimento.Date > DateTime.Today.AddYears(-idade)) idade--;

            var faixaEtaria = ObterFaixaEtariaPorIdade(idade);

            // Buscar categoria compatível
            var categoria = await _context.Categoria.FirstOrDefaultAsync(c =>
                c.CampeonatoId == campeonato.Id &&
                c.Sexo == atleta.Sexo &&
                c.GraduacaoId == atleta.GraduacaoId &&
                c.FaixaEtaria == faixaEtaria &&
                atleta.Peso >= c.PesoMinimo &&
                atleta.Peso <= c.PesoMaximo
            );

            if (categoria == null)
                return BadRequest("Não foi encontrada uma categoria compatível para o atleta. ID=" + atleta.Id);

            var inscricao = new Inscricao
            {
                AtletaId = atleta.Id,
                CampeonatoId = campeonato.Id,
                CategoriaId = categoria.Id,
                DataInscricao = DateTime.UtcNow
            };

            _context.Inscricoes.Add(inscricao);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                mensagem = "Atleta inscrito com sucesso!",
                InscricaoId = inscricao.Id,
                Categoria = categoria.Nome
            });
        }

        // Método auxiliar para obter faixa etária por idade
        private string ObterFaixaEtariaPorIdade(int idade)
        {
            if (idade >= 4 && idade <= 5) return "Pré Mirim";
            if (idade >= 6 && idade <= 7) return "Mirim";
            if (idade >= 8 && idade <= 9) return "Infantil A";
            if (idade >= 10 && idade <= 11) return "Infantil B";
            if (idade >= 12 && idade <= 13) return "Infanto A";
            if (idade >= 14 && idade <= 15) return "Infanto B";
            if (idade >= 16 && idade <= 17) return "Juvenil";
            if (idade >= 18 && idade <= 29) return "Adulto";
            if (idade >= 30 && idade <= 35) return "Master 1";
            if (idade >= 36 && idade >= 40) return "Master 2";
            if (idade >= 41 && idade >= 45) return "Master 3";
            if (idade >= 46 && idade >= 50) return "Master 4";
            if (idade >= 51 && idade >= 55) return "Master 5";
            return "Master 6"; // idade >= 56
        }
    }
}
