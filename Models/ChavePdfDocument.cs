using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using PlataformaAPI.Models;

public class ChavePdfDocument : IDocument
{
    private readonly List<Chave> _chaves;

    public ChavePdfDocument(List<Chave> chaves)
    {
        _chaves = chaves;
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(20);
            page.DefaultTextStyle(x => x.FontSize(12));

            page.Content()
                .Column(column =>
                {
                    foreach (var chave in _chaves)
                    {
                        column.Item().Text($"Chave: {chave.Nome} - Categoria {chave.CategoriaId}").Bold().FontSize(14).Underline();

                        column.Item().Text("Atletas:").Bold();
                        foreach (var inscricao in chave.Inscricoes)
                        {
                            var nome = inscricao.Atleta?.Usuario?.NomeCompleto ?? "(Nome desconhecido)";
                            column.Item().Text($"• {nome}");
                        }

                        column.Item().Text("Lutas:").Bold(); // Removed .PaddingTop(10) as it is not valid for TextBlockDescriptor  
                        column.Item().PaddingTop(10).Column(innerColumn => // Added a new column with PaddingTop to achieve the desired spacing  
                        {
                            foreach (var luta in chave.Lutas)
                            {
                                innerColumn.Item().Text($"- {luta.Atleta1Id} vs {luta.Atleta2Id}");
                            }
                        });

                        column.Item().PaddingVertical(10).LineHorizontal(1);
                    }
                });
        });
    }
}
