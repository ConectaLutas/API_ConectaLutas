using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Mvc;
using PlataformaAPI.Data.Dtos;
using PlataformaAPI.Models;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;
using System.Text;

namespace PlataformaAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UsuarioController : ControllerBase
    {
        private readonly UserManager<Usuario> _userManager;
        private readonly SignInManager<Usuario> _signInManager;
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;

        public UsuarioController(UserManager<Usuario> userManager,
                                 SignInManager<Usuario> signInManager,
                                 IMapper mapper,
                                 IConfiguration configuration)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _mapper = mapper;
            _configuration = configuration;
        }

        

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var usuario = await _userManager.FindByEmailAsync(dto.Email);
            if (usuario == null)
                return Unauthorized(new { mensagem = "E-mail ou senha inválidos." });

            var result = await _signInManager.PasswordSignInAsync(usuario, dto.Password, false, false);

            if (result.Succeeded)
            {
                var token = GenerateJwtToken(usuario);
                return Ok(new { Token = token });
            }

            return Unauthorized(new { mensagem = "E-mail ou senha inválidos." });
        }

        private string GenerateJwtToken(Usuario usuario)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.Id), // ESSA É A CLAIM PADRÃO!
                new Claim("userId", usuario.Id), // Claim customizada simples
                new Claim("email", usuario.Email),
                new Claim("nomeCompleto", usuario.NomeCompleto),
                new Claim("tipoUsuario", usuario.TipoUsuario.ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:SecretKey"]));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(120),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
