using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NoteApp.Domain.Entities;
using NoteApp.Domain.Interfaces;
using NoteApp.Infrastructure.Data;

namespace NoteApp.Infrastructure.Repositories
{
    public class NoteRepository : INoteRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<NoteRepository> _logger;

        public NoteRepository(ApplicationDbContext context, ILogger<NoteRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Note?> GetByIdAsync(Guid id)
        {
            try
            {
                _logger.LogInformation("Fetching note by ID: {NoteId}", id);
                var note = await _context.Notes.FindAsync(id);
                
                if (note == null)
                {
                    _logger.LogInformation("Note with ID {NoteId} not found", id);
                }
                else
                {
                    _logger.LogInformation("Note with ID {NoteId} found", id);
                }
                
                return note;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching note with ID: {NoteId}", id);
                throw new Exception($"Failed to fetch note with ID: {id}", ex);
            }
        }

        public async Task AddAsync(Note note)
        {
            try
            {
                _logger.LogInformation("Adding new note with ID: {NoteId}", note.Id);
                
                // Set any default values not already set
                if (note.CreatedAt == default)
                {
                    note.CreatedAt = DateTime.UtcNow;
                }
                
                await _context.Notes.AddAsync(note);
                
                _logger.LogInformation("Note entity added to context, saving changes");
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Note with ID {NoteId} saved successfully", note.Id);
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error when adding note with ID: {NoteId}", note.Id);
                throw new Exception($"Database error occurred when saving note: {dbEx.InnerException?.Message ?? dbEx.Message}", dbEx);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding note with ID: {NoteId}", note.Id);
                throw new Exception($"Failed to add note with ID: {note.Id}", ex);
            }
        }

        public async Task DeleteAsync(Note note)
        {
            try
            {
                _logger.LogInformation("Removing note with ID: {NoteId}", note.Id);
                _context.Notes.Remove(note);
                
                _logger.LogInformation("Note entity marked for removal, saving changes");
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Note with ID {NoteId} removed successfully", note.Id);
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error when deleting note with ID: {NoteId}", note.Id);
                throw new Exception($"Database error occurred when deleting note: {dbEx.InnerException?.Message ?? dbEx.Message}", dbEx);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting note with ID: {NoteId}", note.Id);
                throw new Exception($"Failed to delete note with ID: {note.Id}", ex);
            }
        }
    }
} 