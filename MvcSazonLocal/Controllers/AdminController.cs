using Microsoft.AspNetCore.Mvc;
using MvcSazonLocal.Filters;
using SazonLocalHelpers.Helpers;
using SazonLocalModels.Models;
using MvcSazonLocal.Services;

namespace MvcSazonLocal.Controllers
{
    [AuthorizeUsuarios(Roles = "ADMINISTRADOR")]
    public class AdminController : Controller
    {
        private SazonApiService serviceApi;
        private HelperPath helper;
        private static string[] extensionesValidas = { ".jpg", ".jpeg", ".png" };

        public AdminController(SazonApiService serviceApi, HelperPath helper)
        {
            this.serviceApi = serviceApi;
            this.helper = helper;
        }

        private IActionResult AjaxOkOrRedirect(string action)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return Ok();
            return RedirectToAction(action);
        }

        #region GESTION MODELOS
        public async Task<IActionResult> GestionModelos()
        {
            AdminConfiguracionModels model = new AdminConfiguracionModels
            {
                Categorias = await this.serviceApi.GetCategoriasAsync(),
                Subcategorias = await this.serviceApi.GetSubcategoriasConCategoriaAsync(),
                UnidadMedidas = await this.serviceApi.GetUnidadesMedidaAsync()
            };
            
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView(model);
            }
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> CrearCategoria(string nombre, string descripcion)
        {
            await this.serviceApi.InsertarCategoriaAsync(nombre, descripcion);
            return AjaxOkOrRedirect("GestionModelos");
        }

        [HttpPost]
        public async Task<IActionResult> CrearSubcategoria(string nombre, string descripcion, IFormFile imagen, int idCategoria)
        {
            string? urlImagen = null;
            if (imagen != null)
            {
                string extension = Path.GetExtension(imagen.FileName).ToLower();
                if (extensionesValidas.Contains(extension))
                {
                    string nombreLimpio = HelperTextCleaner.LimpiarTexto(nombre);
                    urlImagen = nombreLimpio + extension;
                    string path = this.helper.MapPath(urlImagen, Folders.Subcategorias);
                    using (Stream stream = new FileStream(path, FileMode.Create))
                    {
                        await imagen.CopyToAsync(stream);
                    }
                }
            }
            await this.serviceApi.InsertarSubcategoriaAsync(nombre, descripcion, urlImagen, idCategoria);
            return AjaxOkOrRedirect("GestionModelos");
        }

        [HttpPost]
        public async Task<IActionResult> CrearUnidadMedida(string nombre)
        {
            await this.serviceApi.InsertarUnidadMedidaAsync(nombre);
            return AjaxOkOrRedirect("GestionModelos");
        }

        [HttpPost]
        public async Task<IActionResult> CambiarEstado(int id, string tabla)
        {
            if(tabla == "CATEGORIA")
            {
                await this.serviceApi.CambiarEstadoCategoriaAsync(id);
            }
            else if(tabla == "SUBCATEGORIA")
            {
                await this.serviceApi.CambiarEstadoSubcategoriaAsync(id);
            }
            else if(tabla == "UNIDAD_MEDIDA")
            {
                await this.serviceApi.CambiarEstadoUnidadMedidaAsync(id);
            }
            return AjaxOkOrRedirect("GestionModelos");
        }
        #endregion

        #region GESTION PEDIDOS
        public async Task<IActionResult> GestionPedidos()
        {
            List<Pedido> pedidos = await this.serviceApi.GetPedidosAsync();

            decimal totalBruto = pedidos.Sum(p => p.Total);
            decimal totalAgricultores = pedidos.SelectMany(p => p.DetallesPedido).Sum(d => (d.PrecioUnitario * 0.85m) * d.Cantidad);
            decimal totalNeto = totalBruto - totalAgricultores;

            ViewBag.TotalBruto = totalBruto;
            ViewBag.TotalNeto = totalNeto;
            ViewBag.TotalAgricultores = totalAgricultores;
            ViewBag.TotalPedidos = pedidos.Count;
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView(pedidos);
            }
            return View(pedidos);
        }

        public async Task<IActionResult> CambiarEstadoEnCamino(int idPedido)
        {
            await this.serviceApi.CambiarEstadoPedido(idPedido, "EN CAMINO");
            return AjaxOkOrRedirect("GestionPedidos");
        }
        #endregion

        #region GESTIÓN DE FINCAS
        public async Task<IActionResult> GestionFincas(int? estado)
        {
            List<Finca> fincas = await this.serviceApi.GetFincasAdminAsync(estado);
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView(fincas);
            }
            return View(fincas);
        }

        [HttpPost]
        public async Task<IActionResult> ValidarFinca(int idFinca, int nuevoEstado)
        {
            await this.serviceApi.CambiarEstadoValidacionFincaAsync(idFinca, nuevoEstado);
            TempData["Mensaje"] = nuevoEstado == 1 ? "Finca aprobada correctamente." : "Finca rechazada.";
            return AjaxOkOrRedirect("GestionFincas");
        }
        #endregion

        #region GESTIÓN DE USUARIOS (BANEOS)
        public async Task<IActionResult> GestionUsuarios()
        {
            List<Usuario> usuarios = await this.serviceApi.GetUsuariosAsync();
            usuarios = usuarios.Where(u => u.IdRol != 1).ToList();
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView(usuarios);
            }
            return View(usuarios);
        }

        [HttpPost]
        public async Task<IActionResult> CambiarEstadoUsuario(int idUsuario)
        {
            await this.serviceApi.UpdateEstadoUsuarioAsync(idUsuario);
            TempData["Mensaje"] = "Estado del usuario actualizado.";
            return AjaxOkOrRedirect("GestionUsuarios");
        }
        #endregion

    }
}
