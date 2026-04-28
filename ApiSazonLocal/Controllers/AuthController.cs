using ApiSazonLocal.Helpers;
using ApiSazonLocal.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using SazonLocalInterfaces.Interfaces;
using SazonLocalModels.Dto;
using SazonLocalModels.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace ApiSazonLocal.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private IRepository repo;
        private HelperActionOAuth helper;

        public AuthController(IRepository repo, HelperActionOAuth helper)
        {
            this.repo = repo;
            this.helper = helper;
        }

        [HttpPost]
        [Route("[action]")]
        public async Task<ActionResult> Login([FromBody] Login login)
        {
            Usuario user = await this.repo.LogInAsync(login.Email, login.Password);

            if (user == null)
            {
                return Unauthorized();
            }

            SigningCredentials credentials = new SigningCredentials(this.helper.GetTokenKey(), SecurityAlgorithms.HmacSha256);
            UsuarioLogin usuarioLogin = new UsuarioLogin
            {
                IdUsuario = user.IdUsuario,
                Nombre = user.Nombre,
                Email = user.Email,
                IdRol = user.IdRol
            };

            string jsonUsuario = JsonConvert.SerializeObject(usuarioLogin);
            string jsonCifrado = HelperCifrado.CifrarString(jsonUsuario);
            Claim[] informacion = new[]
            {
                    new Claim("UserData", jsonCifrado),
                    new Claim(ClaimTypes.Role, user.Rol.Nombre)
                };

            JwtSecurityToken token = new JwtSecurityToken
            (
                claims: informacion,
                issuer: this.helper.Issuer,
                audience: this.helper.Audience,
                signingCredentials: credentials,
                expires: DateTime.UtcNow.AddMinutes(40),
                notBefore: DateTime.UtcNow
            );

            return Ok(new
            {
                response = new JwtSecurityTokenHandler().WriteToken(token)
            });
        }

        [HttpPost]
        [Route("[action]")]
        public async Task<ActionResult> Register([FromBody] Register model)
        {
            string imagenPorDefecto = "usuario-generico.png";
            int rolFinal = model.IdRol;
            if (rolFinal == 1)
            {
                rolFinal = 2;
            }
            var existente = await this.repo.GetUsuarioByEmailAsync(model.Email);
            if (existente != null)
            {
                return BadRequest("Ese correo electrónico ya está en uso.");
            }

            try
            {
                await this.repo.RegisterUserAsync(model.Nombre, model.Apellidos, model.Email, model.Password, imagenPorDefecto, model.Telefono, rolFinal);
                return Ok("Usuario creado.");
            }
            catch (Exception)
            {
                return StatusCode(500, "Error interno del servidor al procesar el registro.");
            }
        }
    }
}
