using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SazonLocalModels.Models;

public class FacturaDocument : IDocument
{
    public Pedido pedido;
    public string urlImagen;

    private readonly string PrimaryColor = "#556B2f";
    private readonly string AccentColor = "#A0522D";
    private readonly string TextMain = "#2F2F2F";
    private readonly string BgSecondary = "#F5F5DC";
    private readonly string BorderLine = "#E0E0E0";

    public FacturaDocument(Pedido pedido, string urlImagen)
    {
        this.pedido = pedido;
        this.urlImagen = urlImagen;
    }

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Margin(50);
            page.Header().Element(ComposeHeader);
            page.Content().Element(ComposeContent);

            page.Footer().AlignCenter().Text(x => {
                x.Span("Gracias por confiar en el sabor de nuestra tierra").FontSize(10).Italic().FontColor(TextMain);
            });
        });
    }

    void ComposeHeader(IContainer container)
    {
        container.Row(row =>
        {
            row.ConstantItem(100).Column(col =>
            {
                if (System.IO.File.Exists(this.urlImagen))
                {
                    col.Item().Image(this.urlImagen);
                }
            });

            row.RelativeItem().AlignRight().Column(col =>
            {
                col.Item().Text("SAZÓN LOCAL").FontSize(20).SemiBold().FontColor("#556B2f");
                col.Item().Text($"Pedido #{this.pedido.IdPedido}");
                col.Item().Text($"{this.pedido.FechaPedido:dd/MM/yyyy}");
            });
        });
    }

    void ComposeContent(IContainer container)
    {
        container.PaddingVertical(30).Column(col =>
        {
            col.Item().PaddingBottom(20).Row(row =>
            {
                row.RelativeItem().Column(innerCol =>
                {
                    innerCol.Item().Text("DIRECCIÓN DE ENTREGA").FontSize(9).SemiBold().FontColor("#556B2f");

                    innerCol.Item().PaddingTop(2).Text($"{this.pedido.Cliente.Nombre} {this.pedido.Cliente.Apellidos}").FontSize(12).Bold().FontColor("#2F2F2F");

                    innerCol.Item().Text(this.pedido.Direccion.CalleNumero).FontSize(10);
                    innerCol.Item().Text($"{this.pedido.Direccion.CodigoPostal} - {this.pedido.Direccion.Municipio} ({this.pedido.Direccion.Provincia})").FontSize(10);

                    innerCol.Item().PaddingTop(5).Text(x =>
                    {
                        x.Span("Email: ").FontSize(9).SemiBold();
                        x.Span(this.pedido.Cliente.Email).FontSize(9);
                    });

                    if (!string.IsNullOrEmpty(this.pedido.Cliente.Telefono))
                    {
                        innerCol.Item().Text(x =>
                        {
                            x.Span("Tel: ").FontSize(9).SemiBold();
                            x.Span(this.pedido.Cliente.Telefono).FontSize(9);
                        });
                    }
                });
            });

            col.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(30);
                    columns.RelativeColumn(3);
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });

                table.Header(header =>
                {
                    header.Cell().Element(HeaderStyle).Text("#");
                    header.Cell().Element(HeaderStyle).Text("Producto");
                    header.Cell().Element(HeaderStyle).AlignRight().Text("Cant.");
                    header.Cell().Element(HeaderStyle).AlignRight().Text("Precio");
                    header.Cell().Element(HeaderStyle).AlignRight().Text("Total");

                    IContainer HeaderStyle(IContainer c) =>
                        c.DefaultTextStyle(x => x.SemiBold().FontColor(PrimaryColor))
                         .PaddingVertical(5).BorderBottom(1.5f).BorderColor(PrimaryColor);
                });

                foreach (var item in this.pedido.DetallesPedido)
                {
                    table.Cell().Element(CellStyle).Text((this.pedido.DetallesPedido.ToList().IndexOf(item) + 1).ToString());
                    table.Cell().Element(CellStyle).Text(item.Producto.Nombre);
                    table.Cell().Element(CellStyle).AlignRight().Text(item.Cantidad.ToString());
                    table.Cell().Element(CellStyle).AlignRight().Text($"{item.PrecioUnitario:N2}€");
                    table.Cell().Element(CellStyle).AlignRight().Text($"{(item.Cantidad * item.PrecioUnitario):N2}€");

                    IContainer CellStyle(IContainer c) =>
                        c.PaddingVertical(5).BorderBottom(1).BorderColor(BorderLine);
                }
            });

            col.Item().AlignRight().PaddingTop(20).Width(200).Background(BgSecondary).Padding(10).Column(totalCol =>
            {
                totalCol.Item().Row(r => {
                    r.RelativeItem().Text("Subtotal:");
                    r.RelativeItem().AlignRight().Text($"{this.pedido.Subtotal:N2}€");
                });
                totalCol.Item().Row(r => {
                    r.RelativeItem().Text("Gastos Envío:");
                    r.RelativeItem().AlignRight().Text($"{this.pedido.GastosEnvio:N2}€");
                });
                totalCol.Item().Row(r => {
                    r.RelativeItem().Text("Tasa Gestión:");
                    r.RelativeItem().AlignRight().Text($"{this.pedido.TasaGestion:N2}€");
                });

                totalCol.Item().PaddingTop(5).BorderTop(1).BorderColor(PrimaryColor).Row(r => {
                    r.RelativeItem().Text("TOTAL").FontSize(14).SemiBold().FontColor(AccentColor);
                    r.RelativeItem().AlignRight().Text($"{this.pedido.Total:N2}€").FontSize(14).SemiBold().FontColor(AccentColor);
                });
            });
        });
    }
}