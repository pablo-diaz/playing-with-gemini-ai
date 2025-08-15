using System.Text;
using System.Threading.Tasks;

using Build5Nines.SharpVector;
using Build5Nines.SharpVector.Data;

namespace PlayingWithGeminiAI;

// https://sharpvector.build5nines.com/get-started/#basic-example
internal class VectorStoreUsingSharpVectorFromBuild5Nines : IVectorStoreManager
{
    private readonly MemoryVectorDatabase<string> _store = new();
    private readonly TextDataLoader<int, string> _textLoaderIntoStore;

    public VectorStoreUsingSharpVectorFromBuild5Nines()
    {
        _textLoaderIntoStore = new TextDataLoader<int, string>(_store);
    }

    public async Task IndexKnowledge(string contentToIndex)
    {
        await _textLoaderIntoStore.AddDocumentAsync(
            document: contentToIndex,
            chunkingOptions: new TextChunkingOptions<string>
            {
                Method = TextChunkingMethod.OverlappingWindow,
                ChunkSize = 200,
                OverlapSize = 25
            });
    }

    public async Task<string> TryFindInKnowledgeBase(string basedOnUserQuery)
    {
        const string noKnowledgeFound = null;

        const int numberOfResultsToRetrieveFromStore = 2;
        var searchResult = await _store.SearchAsync(queryText: basedOnUserQuery, pageCount: numberOfResultsToRetrieveFromStore);
        if (searchResult.IsEmpty) return noKnowledgeFound;

        var result = new StringBuilder();
        foreach (var item in searchResult.Texts)
        {
            //item.VectorComparison
            result.AppendLine(item.Text);
        }

        return result.Length > 0
            ? result.ToString()
            : noKnowledgeFound;
    }

}
