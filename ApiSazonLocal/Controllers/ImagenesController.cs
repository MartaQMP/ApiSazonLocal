using ApiSazonLocal.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ApiSazonLocal.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ImagenesController : ControllerBase
    {
        private BlobService service;

        public ImagenesController(BlobService service)
        {
            this.service = service;
        }

        [HttpGet]
        public ActionResult GetLogo()
        {
            string urlSas = this.service.GetBlobSasUrl("logo-sl", "Logo_Sazon_Local.png");

            return Ok(new { url = urlSas });
        }
    }
}
