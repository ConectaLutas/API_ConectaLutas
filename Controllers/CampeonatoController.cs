using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlataformaAPI.Data;
using PlataformaAPI.Data.Dtos;
using PlataformaAPI.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PlataformaAPI.Controllers
{
    [ApiController]
    [Route("[Controller]")]
    public class CampeonatoController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public CampeonatoController(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        [HttpPost]
        public async Task<IActionResult> AdicionaCampeonato([FromForm] CreateCampeonatoDto campeonatoDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var campeonato = _mapper.Map<Campeonato>(campeonatoDto);

                if (campeonatoDto.Foto != null)
                {
                    // Defina o caminho para salvar as fotos
                    var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                    if (!Directory.Exists(folderPath))
                        Directory.CreateDirectory(folderPath);

                    var fileName = $"{Guid.NewGuid()}_{campeonatoDto.Foto.FileName}";
                    var filePath = Path.Combine(folderPath, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await campeonatoDto.Foto.CopyToAsync(stream);
                    }

                    // Salva a URL relativa para a foto
                    campeonato.FotoUrl = $"/uploads/{fileName}";
                }

                campeonato.AtualizarStatus();
                _context.Campeonatos.Add(campeonato);
                await _context.SaveChangesAsync();

                var readDto = _mapper.Map<ReadCampeonatoDto>(campeonato);
                return CreatedAtAction(nameof(RecuperarCampeonatoPorId), new { id = campeonato.Id }, readDto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Erro interno: {ex.Message}");
            }
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ReadCampeonatoDto>>> RecuperarCampeonatos([FromQuery] int skip = 0, [FromQuery] int take = 50)
        {
            var campeonatos = await _context.Campeonatos.Skip(skip).Take(take).ToListAsync();

            foreach (var campeonato in campeonatos)
            {
                campeonato.AtualizarStatus();
            }

            return Ok(_mapper.Map<List<ReadCampeonatoDto>>(campeonatos));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> RecuperarCampeonatoPorId(int id)
        {
            var campeonato = await _context.Campeonatos
                .Include(c => c.Inscricoes)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (campeonato == null)
                return NotFound("Campeonato não encontrado.");

            campeonato.AtualizarStatus();

            return Ok(_mapper.Map<ReadCampeonatoDto>(campeonato));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> AtualizaCampeonato(int id, [FromBody] UpdateCampeonatoDto campeonatoDto)
        {
            var campeonato = await _context.Campeonatos.FindAsync(id);
            if (campeonato == null)
                return NotFound("Campeonato não encontrado.");

            _mapper.Map(campeonatoDto, campeonato);
            campeonato.AtualizarStatus();

            try
            {
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Erro ao atualizar: {ex.Message}");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletaCampeonato(int id)
        {
            var campeonato = await _context.Campeonatos.FindAsync(id);
            if (campeonato == null)
                return NotFound("Campeonato não encontrado.");

            _context.Campeonatos.Remove(campeonato);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpGet("{campeonatoId}/inscritos")]
        public async Task<IActionResult> GetAtletasInscritosNoCampeonato(int campeonatoId)
        {
            var campeonatoExiste = await _context.Campeonatos.AnyAsync(c => c.Id == campeonatoId);
            if (!campeonatoExiste)
                return NotFound("Campeonato não encontrado.");

            var atletasInscritos = await _context.Inscricoes
                .Where(i => i.CampeonatoId == campeonatoId)
                .Select(i => new
                {
                    i.AtletaId,
                    NomeAtleta = i.Atleta.Usuario.NomeCompleto,
                    i.DataInscricao
                })
                .ToListAsync();

            return Ok(atletasInscritos);
        }
    }
}
