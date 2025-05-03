using System;

namespace NoteApp.Domain.Entities
{
    public class Note
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ExpiresAt { get; set; }
        public bool IsViewed { get; set; } = false;
    }
} 