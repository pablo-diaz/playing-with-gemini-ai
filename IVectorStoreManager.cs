using System.Threading.Tasks;

namespace PlayingWithGeminiAI;

internal interface IVectorStoreManager
{
    Task IndexKnowledge(string contentToIndex);
    Task<string> TryFindInKnowledgeBase(string basedOnUserQuery);
}
