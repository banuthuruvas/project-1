using Domain.Models;

namespace Domain.Services.Chat;

/// <summary>
/// Chat service — orchestrates conversations, messages, and optional LLM integration.
/// </summary>
public interface IChatService
{
    // Conversations
    Task<List<ChatConversation>> GetConversationsAsync(string userId, string? source = null);
    Task<ChatConversation?> GetConversationAsync(int conversationId);
    Task<ChatConversation> CreateConversationAsync(string userId, string title, string source);
    Task DeleteConversationAsync(int conversationId);
    Task RenameConversationAsync(int conversationId, string newTitle);

    // Messages
    Task<List<ChatMessage>> GetMessagesAsync(int conversationId);
    Task<ChatMessage> SendMessageAsync(int conversationId, string userId, string content);

    // Streaming (SSE)
    IAsyncEnumerable<string> StreamResponseAsync(int conversationId, string userId, string message);
}

/// <summary>
/// Embedding service for semantic search using pgvector.
/// </summary>
public interface IChatEmbeddingService
{
    /// <summary>
    /// Generate an embedding vector for the given text.
    /// </summary>
    Task<float[]> GenerateEmbeddingAsync(string text);

    /// <summary>
    /// Search for similar content using cosine similarity.
    /// </summary>
    Task<List<ChatEmbedding>> SearchSimilarAsync(string query, int topK = 5, string? sourceType = null);

    /// <summary>
    /// Store embeddings for source documents.
    /// </summary>
    Task StoreEmbeddingsAsync(string sourceType, int sourceId, string content, int chunkSize = 500);
}
