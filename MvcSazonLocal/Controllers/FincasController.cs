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
            ViewBag.Direcciones = await this.serviceApi.GetDireccionesUsuarioAsync() ?? new List<Direccion>();
            ViewBag.Fincas = await this.serviceApi.GetFincasActivasAsync();

            if (User.Identity.IsAuthenticated && User.IsInRole("ADMINISTRADOR"))
            {
                ViewBag.FincasPendiente = await this.serviceApi.GetFincasPendientesAsync();
                ViewBag.FincasRechazadas = await this.serviceApi.GetFincasRechazadasAsync();
            }
            else
            {
                ViewBag.FincasPendiente = new List<Finca>();
                ViewBag.FincasRechazadas = new List<Finca>();
            }
            return View();
        }
        #endregion
    }
}
