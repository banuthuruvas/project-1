using System.ComponentModel.DataAnnotations;
using Domain.Models;

namespace Domain.Models;

/// <summary>
/// Chat conversation — groups messages by user, source, and topic.
/// </summary>
public class ChatConversation : TimestampedEntity
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = "New Conversation";

    [Required]
    [MaxLength(100)]
    public string UserId { get; set; } = default!;

    /// <summary>
    /// Source context (e.g., "Procurement", "General", "Documents").
    /// </summary>
    [MaxLength(50)]
    public string Source { get; set; } = "General";

    public DateTime LastMessageAt { get; set; } = DateTime.UtcNow;

    public int MessageCount { get; set; }

    public virtual ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
}

/// <summary>
/// Individual chat message within a conversation.
/// </summary>
public class ChatMessage : TimestampedEntity
{
    [Required]
    [MaxLength(20)]
    public string Role { get; set; } = default!; // "user", "assistant", "system"

    [Required]
    public string Content { get; set; } = default!;

    public int? TokenCount { get; set; }

    /// <summary>
    /// JSON metadata: model used, latency, source documents cited, etc.
    /// </summary>
    public string? Metadata { get; set; }

    public int ConversationId { get; set; }
    public virtual ChatConversation Conversation { get; set; } = default!;
}

/// <summary>
/// Vector embedding for semantic search / RAG.
/// Requires pgvector extension enabled on PostgreSQL.
/// </summary>
public class ChatEmbedding : TimestampedEntity
{
    /// <summary>
    /// Source document chunk text.
    /// </summary>
    [Required]
    public string Content { get; set; } = default!;

    /// <summary>
    /// Embedding vector (pgvector type).
    /// Stored as float[] in code; mapped to vector(1536) in the database.
    /// </summary>
    public float[] Embedding { get; set; } = Array.Empty<float>();

    /// <summary>
    /// Source metadata (document name, page, chunk index).
    /// </summary>
    [MaxLength(500)]
    public string? SourceMetadata { get; set; }

    /// <summary>
    /// Source type (e.g., "PurchaseOrder", "Vendor", "CatalogItem").
    /// </summary>
    [MaxLength(100)]
    public string SourceType { get; set; } = default!;

    public int SourceId { get; set; }
}
