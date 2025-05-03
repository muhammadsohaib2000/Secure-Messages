using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using NoteApp.Domain.Entities;
using NoteApp.Domain.Interfaces;

namespace NoteApp.Application.Services
{

    public class NoteService : INoteService
    {
        private readonly INoteRepository _noteRepo;
        private readonly IDistributedCache _cache;
        private readonly ILogger<NoteService> _logger;
        
        public NoteService(INoteRepository noteRepo, IDistributedCache cache, ILogger<NoteService> logger)
        {
            _noteRepo = noteRepo;
            _cache = cache;
            _logger = logger;
        }

        public async Task<Guid> CreateNoteAsync(string content, int expirationDays = 7)
        {
            try
            {
                _logger.LogInformation("Starting note creation process");
                
                // Create new Note entity
                var note = new Note
                {
                    Content = content,
                    ExpiresAt = DateTime.UtcNow.AddDays(expirationDays)
                };

                _logger.LogInformation("Note entity created with ID: {NoteId}", note.Id);

                // Save to database
                try
                {
                    await _noteRepo.AddAsync(note);
                    _logger.LogInformation("Note saved to database with ID: {NoteId}", note.Id);
                }
                catch (Exception dbEx)
                {
                    _logger.LogError(dbEx, "Error saving note to database: {NoteId}", note.Id);
                    throw new Exception("Failed to save note to database", dbEx);
                }

                // Save to cache with TTL
                try
                {
                    await _cache.SetStringAsync(
                        note.Id.ToString(), 
                        content,
                        new DistributedCacheEntryOptions
                        {
                            AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(expirationDays)
                        });
                    _logger.LogInformation("Note content cached with key: {NoteId}", note.Id);
                }
                catch (Exception cacheEx)
                {
                    _logger.LogWarning(cacheEx, "Failed to cache note, but continuing as database save was successful: {NoteId}", note.Id);
                    // Don't rethrow here as the database operation succeeded
                }

                return note.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error in CreateNoteAsync");
                throw;
            }
        }

        public async Task<string?> ReadNoteAsync(Guid noteId)
        {
            try
            {
                _logger.LogInformation("Attempting to read note with ID: {NoteId}", noteId);

                // Get note from database
                Note? note = null;
                
                try
                {
                    note = await _noteRepo.GetByIdAsync(noteId);
                }
                catch (Exception dbEx)
                {
                    _logger.LogError(dbEx, "Error retrieving note from database: {NoteId}", noteId);
                    throw new Exception("Failed to retrieve note from database", dbEx);
                }
                
                if (note == null)
                {
                    _logger.LogWarning("Note not found in database: {NoteId}", noteId);
                    return null; // Note not found
                }

                _logger.LogInformation("Note found in database: {NoteId}", noteId);

                // Check if already viewed or expired
                if (note.IsViewed)
                {
                    _logger.LogWarning("Note was already viewed: {NoteId}", noteId);
                    return null; // Note was already read
                }

                if (note.ExpiresAt.HasValue && note.ExpiresAt.Value <= DateTime.UtcNow)
                {
                    // Note expired
                    _logger.LogWarning("Note expired: {NoteId}", noteId);
                    try
                    {
                        await _noteRepo.DeleteAsync(note);
                        _logger.LogInformation("Expired note deleted: {NoteId}", noteId);
                    }
                    catch (Exception delEx)
                    {
                        _logger.LogError(delEx, "Error deleting expired note: {NoteId}", noteId);
                        // Continue despite deletion error
                    }
                    
                    return null;
                }

                // Get content - but DON'T delete the note yet
                string content = note.Content;
                
                // IMPORTANT: We no longer delete the note here!
                // Instead, the note will be marked as viewed and deleted when the user confirms
                // they've read it or when they navigate away
                
                _logger.LogInformation("Successfully retrieved note content: {NoteId}", noteId);
                return content;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error in ReadNoteAsync for NoteId: {NoteId}", noteId);
                throw;
            }
        }

        public async Task<bool> MarkNoteAsViewedAsync(Guid noteId)
        {
            try
            {
                _logger.LogInformation("Marking note as viewed: {NoteId}", noteId);
                
                // Get note from database
                var note = await _noteRepo.GetByIdAsync(noteId);
                
                if (note == null)
                {
                    _logger.LogWarning("Cannot mark as viewed - note not found: {NoteId}", noteId);
                    return false;
                }
                
                if (note.IsViewed)
                {
                    _logger.LogInformation("Note was already marked as viewed: {NoteId}", noteId);
                    return true; // Already marked
                }
                
                // Delete the note from database
                try
                {
                    await _noteRepo.DeleteAsync(note);
                    _logger.LogInformation("Note deleted after marking as viewed: {NoteId}", noteId);
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deleting note after marking as viewed: {NoteId}", noteId);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error in MarkNoteAsViewedAsync: {NoteId}", noteId);
                return false;
            }
        }
    }
} 