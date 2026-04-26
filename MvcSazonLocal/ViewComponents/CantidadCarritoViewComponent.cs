using Microsoft.AspNetCore.Mvc;
using MvcSazonLocal.Extensions;
using MvcSazonLocal.Services;
using SazonLocalModels.Models;
using System.Security.Claims;

namespace MvcSazonLocal.ViewComponents
{
    public class CantidadCarritoViewComponent: ViewComponent
    {
        private SazonApiService service;

        public CantidadCarritoViewComponent(SazonApiService service)
        {
            this.service = service;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            int totalProductos = 0;
            string claimId = this.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            int? idUsuario = null;
            if (claimId != null)
            {
                idUsuario = int.Parse(claimId);
            }

            if (idUsuario == null)
            {
                Dictionary<int, int> carrito = HttpContext.Session.GetObject<Dictionary<int, int>>("CARRITO");
                if (carrito != null)
                {
                    totalProductos = carrito.Values.Sum();
                }
            }
            else
            {
                List<CarritoItem> carrito = await this.service.GetCarritoUsuarioAsync(idUsuario.Value);
                if (carrito != null)
                {
                    totalProductos = carrito.Sum(c => c.Cantidad);
                }
            }
            return View(totalProductos);
        }
    }
}
