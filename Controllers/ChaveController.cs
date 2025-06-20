using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlataformaAPI.Data;
using PlataformaAPI.Models;
using PlataformaJiujitsu.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Text;

[ApiController]
[Route("api/[controller]")]
public class ChaveController : ControllerBase
{
    private readonly UserManager<Usuario> _userManager;
    private readonly ApplicationDbContext _context;

    public ChaveController(UserManager<Usuario> userManager, ApplicationDbContext context)
    {
        _userManager = userManager;
        _context = context;
    }

    [HttpPost("{campeonatoId}/gerar-chave")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<IActionResult> GerarChave([FromRoute] int campeonatoId)
    {
        var usuarioAtual = await _userManager.GetUserAsync(User);
        if (usuarioAtual == null || usuarioAtual.TipoUsuario != TipoUsuario.Administrador)
            return Unauthorized("Somente administradores podem gerar chaves.");

        var campeonato = await _context.Campeonatos
            .Include(c => c.Categorias)
            .Include(c => c.Inscricoes)
                .ThenInclude(i => i.Atleta)
                    .ThenInclude(a => a.Usuario)
            .FirstOrDefaultAsync(c => c.Id == campeonatoId);

        if (campeonato == null)
            return NotFound("Campeonato não encontrado.");

        if (campeonato.Status != StatusCampeonato.EmAndamento)
            return BadRequest("O campeonato não está em andamento.");

        var inscricoes = campeonato.Inscricoes.ToList();

        foreach (var i in inscricoes)
        {
            if (i.Atleta == null || i.Atleta.Usuario == null || string.IsNullOrWhiteSpace(i.Atleta.Usuario.NomeCompleto))
                return BadRequest("Todos os atletas devem estar com seus dados completos.");
        }

        var chaves = GerarChavesParaCampeonato(campeonato);

        if (!chaves.Any())
            return BadRequest("Não há inscrições suficientes para gerar chaves.");

        _context.Chaves.AddRange(chaves);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            mensagem = "Chaves geradas com sucesso!",
            chaves = chaves.Select(chave => new
            {
                chave.CategoriaId,
                chave.Nome,
                AtletasInscritos = chave.Inscricoes.Select(i => new
                {
                    i.AtletaId,
                    Nome = i.Atleta.Usuario.NomeCompleto
                }),
                Lutas = chave.Lutas.Select(l => new
                {
                    l.Atleta1Id,
                    l.Atleta2Id
                })
            })
        });
    }

    private List<Luta> GerarLutas(List<Inscricao> inscricoes)
    {
        var lutas = new List<Luta>();
        var atletas = inscricoes.Select(i => i.Atleta).ToList();

        var random = new Random();
        atletas = atletas.OrderBy(a => random.Next()).ToList();

        for (int i = 0; i < atletas.Count - 1; i += 2)
        {
            lutas.Add(new Luta
            {
                Atleta1Id = atletas[i].Id,
                Atleta2Id = atletas[i + 1].Id
            });
        }

        if (atletas.Count % 2 != 0)
        {
            var atletaBye = atletas.Last();
            lutas.Add(new Luta
            {
                Atleta1Id = atletaBye.Id,
                Atleta2Id = atletaBye.Id
            });
        }

        return lutas;
    }

    private List<Chave> GerarChavesParaCampeonato(Campeonato campeonato)
    {
        var chaves = new List<Chave>();
        var categorias = campeonato.Categorias.ToList();
        var inscricoes = campeonato.Inscricoes.ToList();

        foreach (var categoria in categorias)
        {
            var inscritosNaCategoria = inscricoes
                .Where(i => i.CategoriaId == categoria.Id)
                .ToList();

            if (inscritosNaCategoria.Count >= 2)
            {
                var chave = new Chave
                {
                    CategoriaId = categoria.Id,
                    Nome = $"Chave_{categoria.Id}",
                    Inscricoes = inscritosNaCategoria,
                    Lutas = GerarLutas(inscritosNaCategoria)
                };

                chaves.Add(chave);
            }
        }

        return chaves;
    }

    [HttpGet("{campeonatoId}/chaves")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<IActionResult> GetChaves([FromRoute] int campeonatoId)
    {
        var campeonato = await _context.Campeonatos
            .Include(c => c.Categorias)
            .FirstOrDefaultAsync(c => c.Id == campeonatoId);

        if (campeonato == null)
            return NotFound("Campeonato não encontrado.");

        var chaves = await _context.Chaves
            .Include(ch => ch.Inscricoes)
                .ThenInclude(i => i.Atleta)
                    .ThenInclude(a => a.Usuario)
            .Include(ch => ch.Lutas)
            .Where(ch => ch.Inscricoes.Any(i => i.CampeonatoId == campeonatoId))
            .ToListAsync();

        if (!chaves.Any())
            return NotFound("Nenhuma chave gerada ainda.");

        var chavesResponse = chaves.Select(chave => new
        {
            chave.CategoriaId,
            chave.Nome,
            AtletasInscritos = chave.Inscricoes.Select(i => new
            {
                i.AtletaId,
                Nome = i.Atleta.Usuario.NomeCompleto
            }),
            Lutas = chave.Lutas.Select(l => new
            {
                l.Atleta1Id,
                l.Atleta2Id
            })
        });

        return Ok(new
        {
            campeonatoId = campeonato.Id,
            chaves = chavesResponse
        });
    }

    [HttpGet("{campeonatoId}/chaves/pdf")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<IActionResult> GerarPdfChaves(int campeonatoId)
    {
        try
        {
            var chaves = await _context.Chaves
                .Include(ch => ch.Inscricoes)
                    .ThenInclude(i => i.Atleta)
                        .ThenInclude(a => a.Usuario)
                .Include(ch => ch.Lutas)
                .Where(ch => ch.Inscricoes.Any(i => i.CampeonatoId == campeonatoId))
                .ToListAsync();

            if (!chaves.Any())
                return NotFound("Nenhuma chave encontrada.");

            byte[] pdfBytes = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(30);
                    page.Content().Column(col =>
                    {
                        col.Item().Text($"Chaves do Campeonato {campeonatoId}")
                            .FontSize(20)
                            .Bold()
                            .AlignCenter();

                        foreach (var chave in chaves)
                        {
                            col.Item().PaddingTop(20).Column(chaveCol =>
                            {
                                chaveCol.Item().Text($"Chave: {chave.Nome}")
                                    .FontSize(16)
                                    .Bold();

                                chaveCol.Item().Text("Atletas:");
                                foreach (var i in chave.Inscricoes ?? new List<Inscricao>())
                                {
                                    var nome = i?.Atleta?.Usuario?.NomeCompleto ?? "Desconhecido";
                                    chaveCol.Item().Text($"- {nome}");
                                }

                                chaveCol.Item().Text("Lutas:");
                                foreach (var luta in chave.Lutas ?? new List<Luta>())
                                {
                                    var atleta1 = chave.Inscricoes.FirstOrDefault(i => i.AtletaId == luta.Atleta1Id)?.Atleta?.Usuario?.NomeCompleto ?? "Desconhecido";
                                    var atleta2 = chave.Inscricoes.FirstOrDefault(i => i.AtletaId == luta.Atleta2Id)?.Atleta?.Usuario?.NomeCompleto ?? "Desconhecido";
                                    chaveCol.Item().Text($"{atleta1} vs {atleta2}");
                                }

                                chaveCol.Item().Element(e => e.LineHorizontal(1).LineColor(Colors.Grey.Lighten2));
                            });
                        }
                    });
                });
            }).GeneratePdf();

            return File(pdfBytes, "application/pdf", $"chaves_campeonato_{campeonatoId}.pdf");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex); // ou log
            return StatusCode(500, $"Erro interno ao gerar PDF: {ex.Message}");
        }
    }

}
