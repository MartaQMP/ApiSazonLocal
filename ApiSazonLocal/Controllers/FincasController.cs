using ApiSazonLocal.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SazonLocalModels.Models;
using SazonLocalInterfaces.Interfaces;
using SazonLocalModels.Dto;
using Microsoft.AspNetCore.Authorization;
using ApiSazonLocal.Helpers;

namespace ApiSazonLocal.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FincasController : ControllerBase
    {
        private IRepository repo;
        private HelperToken helper;

        public FincasController(IRepository repo, HelperToken helper)
        {
            this.repo = repo;
            this.helper = helper;
        }

        [HttpGet]
        [Route("[action]")]
        public async Task<ActionResult<List<Finca>>> GetFincasActivas()
        {
            List<Finca> fincas = await this.repo.GetFincasActivasAsync();
            return Ok(fincas);
        }

        [Authorize(Roles = "ADMINISTRADOR")]
        [HttpGet]
        [Route("[action]")]
        public async Task<ActionResult<List<Finca>>> GetFincasPendientes()
        {
            List<Finca> fincas = await this.repo.GetFincasPendientesAsync();
            return Ok(fincas);
        }

        [Authorize(Roles = "ADMINISTRADOR")]
        [HttpGet]
        [Route("[action]")]
        public async Task<ActionResult<List<Finca>>> GetFincasRechazadas()
        {
            List<Finca> fincas = await this.repo.GetFincasRechazadasAsync();
            return Ok(fincas);
        }

        [Authorize]
        [HttpGet]
        [Route("[action]/Usuario")]
        public async Task<ActionResult<List<Finca>>> GetFincas()
        {
            UsuarioLogin usuario = this.helper.GetUsuario();
            List<Finca> fincas = await this.repo.GetFincasUsuarioAsync(usuario.IdUsuario);
            return Ok(fincas);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Finca>> GetFinca(int id)
        {
            Finca finca = await this.repo.GetFincaByIdAsync(id);
            if (finca == null)
            {
                return NotFound(new { mensaje = $"La finca con ID {id} no existe." });
            }
            return Ok(finca);
        }

        [HttpGet]
        [Route("[action]/{estado}")]
        public async Task<ActionResult<List<Finca>>> GetFincasAdmin(int? estado)
        {
            List<Finca> fincas = await this.repo.GetFincasAdminAsync(estado);
            return Ok(fincas);
        }

        [Authorize]
        [HttpPost]
        public async Task<ActionResult> Post([FromBody] FincaDto finca)
        {
            UsuarioLogin usuario = this.helper.GetUsuario();
            try
            {
                await this.repo.InsertarFincaAsync(finca.Nombre, finca.Direccion, finca.Municipio, finca.Provincia, finca.Latitud, finca.Longitud, usuario.IdUsuario);
                return Ok(new { mensaje = "Finca creada correctamente." });
            }
            catch (Exception)
            {
                return BadRequest("Error al insertar la finca.");
            }
        }

        [Authorize]
        [HttpPut]
        [Route("[action]/{id}")]
        public async Task<ActionResult> Actualizar(int id, [FromBody] FincaDto finca)
        {
            UsuarioLogin usuario = this.helper.GetUsuario();
            Finca fincaId = await this.repo.GetFincaByIdAsync(id);
            if (fincaId == null)
            {
                return NotFound(new { mensaje = "Finca no encontrada." });
            }

            try
            {
                await this.repo.ActualizarFincaAsync(id, finca.Nombre, finca.Direccion, finca.Municipio, finca.Provincia, finca.Latitud, finca.Longitud, usuario.IdUsuario);
                return Ok(new { mensaje = "Finca actualizada correctamente." });
            }
            catch (Exception)
            {
                return StatusCode(500, "Error al actualizar la finca.");
            }
        }

        [Authorize]
        [HttpPut]
        [Route("[action]/{id}")]
        public async Task<ActionResult> CambiarEstado(int id)
        {
            var finca = await this.repo.GetFincaByIdAsync(id);
            if (finca == null)
            {
                return NotFound(new { mensaje = "Finca no encontrada." });
            }

            try
            {
                await this.repo.CambiarEstadoFincaAsync(id);
                return Ok(new { mensaje = "Estado de la finca actualizado." });
            }
            catch (Exception)
            {
                return StatusCode(500, "Error al cambiar el estado.");
            }
        }

        [Authorize(Roles = "ADMINISTRADOR")]
        [HttpPut]
        [Route("[action]/{id}/{nuevoEstado}")]
        public async Task<ActionResult> CambiarEstadoValidacion(int id, int nuevoEstado)
        {
            var finca = await this.repo.GetFincaByIdAsync(id);
            if (finca == null)
            {
                return NotFound(new { mensaje = "Finca no encontrada." });
            }

            try
            {
                await this.repo.CambiarEstadoValidacionFincaAsync(id, nuevoEstado);
                return Ok(new { mensaje = "Estado de validación actualizado." });
            }
            catch (Exception)
            {
                return StatusCode(500, "Error al cambiar el estado de validación.");
            }
        }
    }
}
