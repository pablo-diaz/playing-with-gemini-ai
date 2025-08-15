using System.Text;
using System.Threading.Tasks;

using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace PlayingWithGeminiAI;

internal class PdfContentRetriever
{
    public static Task<string> GetContent(string fromPdfFile)
    {
        using var document = PdfDocument.Open(fromPdfFile);
        var result = new StringBuilder();
        foreach (var page in document.GetPages())
        {
            result.AppendLine(ContentOrderTextExtractor.GetText(page));
        }

        return Task.FromResult(result.ToString());
    }

}
