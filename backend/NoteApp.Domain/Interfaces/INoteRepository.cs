using System;
using System.Threading.Tasks;
using NoteApp.Domain.Entities;

namespace NoteApp.Domain.Interfaces
{
    public interface INoteRepository
    {
        Task<Note?> GetByIdAsync(Guid id);
        Task AddAsync(Note note);
        Task DeleteAsync(Note note);
    }
} 