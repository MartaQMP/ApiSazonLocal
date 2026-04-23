using Microsoft.AspNetCore.Mvc;
using SazonLocalModels.Models;
using MvcSazonLocal.Services;
using System.Security.Claims;

namespace MvcSazonLocal.Controllers
{
    public class FincasController : Controller
    {
        private SazonApiService serviceApi;

        public FincasController(SazonApiService serviceApi)
        {
            this.serviceApi = serviceApi;
        }


        #region FINCAS
        public async Task<IActionResult> Fincas()
        {
            string claimId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int? idUsuario = null;
            if (claimId != null)
            {
                idUsuario = int.Parse(claimId);
            }
            var direcciones = (idUsuario != null)
                    ? await this.serviceApi.GetDireccionesUsuarioAsync(idUsuario.Value)
                    : new List<Direccion>();

            ViewBag.Direcciones = direcciones;
            ViewBag.Fincas = await this.serviceApi.GetFincasActivasAsync();
            ViewBag.FincasPendiente = await this.serviceApi.GetFincasPendientesAsync();
            ViewBag.FincasRechazadas = await this.serviceApi.GetFincasRechazadasAsync();
            return View();
        }
        #endregion
    }
}
