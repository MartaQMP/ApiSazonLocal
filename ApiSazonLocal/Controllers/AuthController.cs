using ApiSazonLocal.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SazonLocalInterfaces.Interfaces;
using SazonLocalModels.Dto;
using SazonLocalModels.Models;

namespace ApiSazonLocal.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private IRepository repo;

        public AuthController(IRepository repo)
        {
            this.repo = repo;
        }

        [HttpPost]
        [Route("[action]")]
        public async Task<ActionResult> Login([FromBody] Login login)
        {
            Usuario user = await this.repo.LogInAsync(login.Email, login.Password);

            if (user == null)
            {
                return Unauthorized("Credenciales inválidas.");
            }

            return Ok(user);
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
