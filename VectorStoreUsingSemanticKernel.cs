using System;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Connectors.InMemory;

namespace PlayingWithGeminiAI;

internal class VectorStoreUsingSemanticKernel : IVectorStoreManager
{
    public static readonly IDictionary<string, object> UseItForIndexingPurposes = new Dictionary<string, object>() {
        { "task_type", "RETRIEVAL_DOCUMENT" }
    };

    public const int EmbedingSize = 1536; // https://ai.google.dev/gemini-api/docs/embeddings#control-embedding-size

    private int keyCount = 0;

    private readonly GetVectorRepresentation _getVectorRepresentationFn;
    private readonly InMemoryVectorStore _vectorStore = new();
    private readonly VectorStoreCollection<int, VectorStoreEntry> _knowledgeCollection;

    public delegate Task<ReadOnlyMemory<float>> GetVectorRepresentation(string fromText);
    private sealed record Chunk(string Content);

    private sealed class VectorStoreEntry
    {
        [VectorStoreKey]
        public int Key { get; set; }

        [VectorStoreData]
        public string Content { get; set; }

        [VectorStoreVector(Dimensions: EmbedingSize, DistanceFunction = DistanceFunction.CosineSimilarity)]
        public ReadOnlyMemory<float> VectorRepresentation { get; set; }
    }

    public VectorStoreUsingSemanticKernel(GetVectorRepresentation getVectorRepresentationFn)
    {
        _getVectorRepresentationFn = getVectorRepresentationFn;
        _knowledgeCollection = _vectorStore.GetCollection<int, VectorStoreEntry>("knowledgeBase");
        _knowledgeCollection.EnsureCollectionExistsAsync().GetAwaiter().GetResult();
    }

    public async Task IndexKnowledge(string contentToIndex)
    {
        await IndexChunksInKnowledgeBase(
                chunksToIndex: GetChunks(
                    fromContent: contentToIndex,
                    withWordCountPerChunk: 200,
                    withChunkOverlappingPercentage: 30.0));
    }

    public async Task<string> TryFindInKnowledgeBase(string basedOnUserQuery)
    {
        // https://learn.microsoft.com/en-us/dotnet/ai/quickstarts/build-vector-search-app?pivots=openai

        const string noKnowledgeFound = null;
        var numberOfResultsToReturn = 2;

        var results = _knowledgeCollection.SearchAsync(
            searchValue: await _getVectorRepresentationFn(basedOnUserQuery),
            top: numberOfResultsToReturn);

        var searchResults = new StringBuilder();
        await foreach (var result in results)
        {
            var isItAValidMatch = result.Score >= 0.7;
            if (!isItAValidMatch) continue;

            searchResults.AppendLine(result.Record.Content);
        }

        return searchResults.Length > 0
            ? searchResults.ToString()
            : noKnowledgeFound;
    }

    private int ProvideNextSequenceKeyForVectorStoreEntries() => ++keyCount;

    private async Task IndexChunksInKnowledgeBase(IEnumerable<Chunk> chunksToIndex)
    {
        foreach (var chunk in chunksToIndex)
        {
            await IndexVectorEntryInKnowledgeBase(entry: await CreateVectorStoreEntry(
                fromChunk: chunk,
                withKey: ProvideNextSequenceKeyForVectorStoreEntries()));
        }
    }

    private async Task<VectorStoreEntry> CreateVectorStoreEntry(Chunk fromChunk, int withKey) =>
        new VectorStoreEntry
        {
            Key = withKey,
            Content = fromChunk.Content,
            VectorRepresentation = await _getVectorRepresentationFn(fromText: fromChunk.Content)
        };

    private async Task IndexVectorEntryInKnowledgeBase(VectorStoreEntry entry)
    {
        await _knowledgeCollection.UpsertAsync(entry);
    }

    private IEnumerable<Chunk> GetChunks(string fromContent, int withWordCountPerChunk, double withChunkOverlappingPercentage)
    {
        var words = fromContent.Split([' ', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries);
        var totalWords = words.Length;
        var overlapCount = (int)(withWordCountPerChunk * (withChunkOverlappingPercentage / 100.0));
        for (var i = 0; i < totalWords; i += withWordCountPerChunk - overlapCount)
        {
            var areThereEnoughWordsLeftToMakeItForChunkExpectedSize = i + withWordCountPerChunk <= totalWords;
            if (areThereEnoughWordsLeftToMakeItForChunkExpectedSize)
                yield return new Chunk(Content: string.Join(" ", words, i, withWordCountPerChunk));
            else
                yield return new Chunk(Content: string.Join(" ", words, i, totalWords - i));
        }
    }

}
