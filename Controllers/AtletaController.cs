using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PlataformaAPI.Data;
using PlataformaAPI.Models;
using PlataformaJiujitsu.Data.Dtos;
using PlataformaJiujitsu.Models;
using Microsoft.AspNetCore.Authorization;

namespace PlataformaJiujitsu.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AtletaController : ControllerBase
    {
        private readonly UserManager<Usuario> _userManager;
        private readonly ApplicationDbContext _context;

        public AtletaController(UserManager<Usuario> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        [HttpPost("cadastrar-atleta")]
        public async Task<IActionResult> CadastrarAtleta(CreateAtletaUsuarioDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Verificar se o e-mail já está cadastrado
            var usuarioExistente = await _userManager.FindByEmailAsync(dto.Email);
            if (usuarioExistente != null)
                return Conflict(new { mensagem = "E-mail já cadastrado." });

            // Criar usuário
            var usuario = new Usuario
            {
                Email = dto.Email,
                UserName = dto.Email, // <<< Adicione isso!
                NomeCompleto = dto.NomeCompleto,
                DataNascimento = dto.DataNascimento,
                CPF = dto.CPF,
                TipoUsuario = TipoUsuario.Atleta
            };


            var result = await _userManager.CreateAsync(usuario, dto.Password);
            if (!result.Succeeded)
                return BadRequest(new { erros = result.Errors.Select(e => e.Description) });

            // Criar Atleta
            var atleta = new Atleta
            {
                UsuarioId = usuario.Id,
                GraduacaoId = dto.GraduacaoId,
                Peso = dto.Peso,
                Sexo = dto.Sexo,
                EsporteId = dto.EsporteId,
                AcademiaId = dto.AcademiaId,
                ProfessorId = dto.ProfessorId,
                DataNascimento = dto.DataNascimento,
            };

            _context.Atletas.Add(atleta);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(CadastrarAtleta), new
            {
                mensagem = "Atleta cadastrado com sucesso!",
                atleta.Id,
                usuario.Email,
                atleta.GraduacaoId,
                atleta.Peso
            });
        }


        [HttpGet]
        public async Task<ActionResult<IEnumerable<Atleta>>> GetAtletas()
        {
            var atletas = await _context.Atletas
                .Include(a => a.Usuario) // Inclui os dados do usuário
                .ToListAsync();

            return Ok(atletas);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Atleta>> GetAtletaPorId(int id)
        {
            var atleta = await _context.Atletas
                .Include(a => a.Usuario)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (atleta == null)
                return NotFound("Atleta não encontrado.");

            return Ok(atleta);
        }

        [HttpGet("por-usuario/{usuarioId}")]
        public async Task<ActionResult<Atleta>> GetAtletaPorUsuarioId(string usuarioId)
        {
            var atleta = await _context.Atletas
                .Include(a => a.Usuario)
                .FirstOrDefaultAsync(a => a.UsuarioId == usuarioId);

            if (atleta == null)
                return NotFound("Atleta não encontrado para este usuário.");

            return Ok(atleta);
        }
    }
}
