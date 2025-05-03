using System;
using System.Threading.Tasks;

namespace NoteApp.Application.Services
{
    public interface INoteService
    {
        Task<Guid> CreateNoteAsync(string content, int expirationDays = 7);
        Task<string?> ReadNoteAsync(Guid noteId);
        Task<bool> MarkNoteAsViewedAsync(Guid noteId);
    }
} 