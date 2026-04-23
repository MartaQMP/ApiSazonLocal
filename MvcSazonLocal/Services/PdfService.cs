using QuestPDF.Fluent;
using SazonLocalHelpers.Helpers;
using SazonLocalInterfaces.Services;
using SazonLocalModels.Models;

namespace MvcSazonLocal.Services
{
    public class PdfService: IPdfService
    {
        private HelperPath helper;

        public PdfService(HelperPath helper)
        {
            this.helper = helper;
        }

        public byte[] GenerarFacturaPedido(Pedido pedido)
        {
            string urlImagen = helper.MapPath("Logo_Sazon_Local.png", Folders.Images);
            var documento = new FacturaDocument(pedido, urlImagen);
            return documento.GeneratePdf();
        }
    }
}
