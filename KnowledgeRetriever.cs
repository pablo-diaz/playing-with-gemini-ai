using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace PlayingWithGeminiAI;

internal class KnowledgeRetriever
{
    private readonly IVectorStoreManager _storeManager;

    public abstract record KnowledgeEntry();
    public sealed record KnowledgeWasNotFound() : KnowledgeEntry();
    public sealed record KnowledgeFound(string TextOfKnowledge) : KnowledgeEntry()
    {
        public override string ToString() => TextOfKnowledge;
    }

    private sealed record ContentPresentInFile(string Filename, string Content);

    public KnowledgeRetriever(IVectorStoreManager storeManager)
    {
        this._storeManager = storeManager;
    }

    public async Task StartIndexingKnowledge()
    {
        await IndexKnowledge(streamOfContentOfFiles: ScanFiles(presentInFolder: @"C:\tmp\rag"));
    }

    public async Task<KnowledgeEntry> TryFindInKnowledgeBase(string basedOnUserQuery)
    {
        var maybeKnowledgeFound = await _storeManager.TryFindInKnowledgeBase(basedOnUserQuery);

        return !string.IsNullOrEmpty(maybeKnowledgeFound)
            ? new KnowledgeFound(TextOfKnowledge: maybeKnowledgeFound)
            : new KnowledgeWasNotFound();
    }

    private static async IAsyncEnumerable<ContentPresentInFile> ScanFiles(string presentInFolder)
    {
        foreach (var filename in Directory.GetFiles(presentInFolder, "*.pdf"))
        {
            yield return new ContentPresentInFile(Filename: filename, Content: await PdfContentRetriever.GetContent(fromPdfFile: filename));
        }
    }

    private async Task IndexKnowledge(IAsyncEnumerable<ContentPresentInFile> streamOfContentOfFiles)
    {
        await foreach (var fileContent in streamOfContentOfFiles)
        {
            await _storeManager.IndexKnowledge(contentToIndex: fileContent.Content);
        }
    }

}
