using Microsoft.AspNetCore.Mvc;
using MvcSazonLocal.Extensions;
using SazonLocalHelpers.Helpers;
using SazonLocalModels.Models;
using MvcSazonLocal.Services;
using System.Security.Claims;

namespace MvcSazonLocal.Controllers
{
    public class ProductosController : Controller
    {
        private readonly SazonApiService serviceApi;

        public ProductosController(SazonApiService serviceApi)
        {
            this.serviceApi = serviceApi;
        }

        #region PRODUCTOS
        public async Task<IActionResult> Productos(int posicion = 0, string? buscador = null, int? idCategoria = null, int? idSubcategoria = null, int? idFinca = null, decimal? precio = null)
        {
            string? claimId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int? idUsuario = null;
            if (claimId != null)
            {
                idUsuario = int.Parse(claimId);
            }
            Dictionary<int, int> cantidadesEnCarrito = new Dictionary<int, int>();

            if (idUsuario == null)
            {
                cantidadesEnCarrito = HttpContext.Session.GetObject<Dictionary<int, int>>("CARRITO");
            }
            else
            {
                List<CarritoItem> carritoItems = await this.serviceApi.GetCarritoUsuarioAsync();
                cantidadesEnCarrito = carritoItems.GroupBy(c => c.IdProducto).ToDictionary(g => g.Key, g => g.Sum(c => c.Cantidad));
            }
            ViewBag.CantidadesCarrito = cantidadesEnCarrito;

            ProductosPaginacion productosPaginacion = await this.serviceApi.GetProductosFiltroAsync(posicion, buscador, idCategoria, idSubcategoria, idFinca, precio)
                ?? new ProductosPaginacion();
            productosPaginacion.Productos ??= new List<Producto>();

            ViewBag.Buscador = buscador;
            ViewBag.IdCategoria = idCategoria;
            ViewBag.IdSubcategoria = idSubcategoria;
            ViewBag.IdFinca = idFinca;
            ViewBag.PrecioMax = precio;
            ViewBag.Categorias = (await this.serviceApi.GetCategoriasAsync()).Where(c => c.EstaActiva).ToList();
            ViewBag.Subcategorias = (await this.serviceApi.GetSubcategoriasAsync()).Where(s => s.EstaActiva).ToList();
            ViewBag.Fincas = await this.serviceApi.GetFincasActivasAsync();
            int salto = 40;
            ViewBag.Actual = posicion;
            ViewBag.Total = productosPaginacion.TotalProductos;
            ViewBag.Siguiente = posicion + salto;
            ViewBag.Anterior = posicion - salto;
            ViewBag.Primero = 0;
            ViewBag.Ultimo = productosPaginacion.NumeroRegistros > 0 ? (productosPaginacion.NumeroRegistros - 1) * salto : 0;

            return View(productosPaginacion.Productos);
        }

        [HttpPost]
        public async Task<IActionResult> AnadirCarrito(int idProducto, int cantidad, bool esCarrito)
        {
            string? claimId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int? idUsuario = null;
            if (claimId != null)
            {
                idUsuario = int.Parse(claimId);
            }
            if (idUsuario == null)
            {
                Dictionary<int, int> carrito;
                if (HttpContext.Session.GetObject<Dictionary<int, int>>("CARRITO") != null)
                { 
                    carrito = HttpContext.Session.GetObject<Dictionary<int, int>>("CARRITO");
                }
                else
                {
                    carrito = new Dictionary<int, int>();
                }
                carrito[idProducto] = cantidad;
                HttpContext.Session.SetObject("CARRITO", carrito);
            }
            else
            {
                CarritoItem existeProducto = await this.serviceApi.GetProductoCarritoAsync(idProducto);
                if (existeProducto != null)
                {
                    await this.serviceApi.ActualizarCantidadCarritoAsync(idProducto, cantidad);
                }
                else
                {
                    await this.serviceApi.InsertarProductoCarritoAsync(cantidad, idProducto, null);
                }
            }
            if (esCarrito)
            {
                return RedirectToAction("Carrito", "Compra");
            }
            else
            {
                int nuevoTotal = 0;
                if (idUsuario == null)
                {
                    var carrito = HttpContext.Session.GetObject<Dictionary<int, int>>("CARRITO");
                    nuevoTotal = carrito?.Values.Sum() ?? 0;
                }
                else
                {
                    var carritoItems = await this.serviceApi.GetCarritoUsuarioAsync();
                    nuevoTotal = carritoItems.Sum(c => c.Cantidad);
                }

                return Ok(nuevoTotal);
            }
        }
        #endregion
    }
}
