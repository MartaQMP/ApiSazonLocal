using Microsoft.AspNetCore.Mvc;

namespace MvcSazonLocal.Controllers
{
    public class LegalController : Controller
    {

        #region POLITICA PRIVACIDAD
        public IActionResult PoliticaPrivacidad()
        {
            return View();
        }
        #endregion

        #region TERMINOS SERVICIO
        public IActionResult TerminosServicio()
        {
            return View();
        }
        #endregion
    }
}
