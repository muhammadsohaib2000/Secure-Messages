using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NoteApp.Application.Services;

namespace NoteApp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotesController : ControllerBase
    {
        private readonly INoteService _noteService;
        private readonly ILogger<NotesController> _logger;

        public NotesController(INoteService noteService, ILogger<NotesController> logger)
        {
            _noteService = noteService;
            _logger = logger;
        }

        // POST: api/notes
        [HttpPost]
        public async Task<IActionResult> CreateNote([FromBody] CreateNoteDto dto)
        {
            try
            {
                _logger.LogInformation("Received request to create a new note");
                
                if (string.IsNullOrWhiteSpace(dto.Content))
                {
                    _logger.LogWarning("Note creation rejected: Empty content");
                    return BadRequest("Note content cannot be empty.");
                }

                _logger.LogInformation("Creating new note with content length: {ContentLength}", dto.Content.Length);
                Guid id = await _noteService.CreateNoteAsync(dto.Content);
                
                _logger.LogInformation("Note created successfully with ID: {NoteId}", id);
                return Ok(new { noteId = id.ToString() });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating note");
                return StatusCode(500, new { 
                    error = "Error creating note", 
                    message = ex.Message,
                    innerError = ex.InnerException?.Message 
                });
            }
        }

        // GET: api/notes/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetNote(string id)
        {
            try
            {
                _logger.LogInformation("Received request to retrieve note with ID: {NoteId}", id);
                
                if (!Guid.TryParse(id, out Guid noteId))
                {
                    _logger.LogWarning("Invalid note ID format: {NoteId}", id);
                    return BadRequest("Invalid note ID format.");
                }

                _logger.LogInformation("Attempting to read note with ID: {NoteId}", noteId);
                string? content = await _noteService.ReadNoteAsync(noteId);
                
                if (content == null)
                {
                    _logger.LogInformation("Note not found or already destroyed: {NoteId}", noteId);
                    return NotFound("Note not found or already destroyed.");
                }

                _logger.LogInformation("Successfully retrieved note: {NoteId}, Content length: {ContentLength}", 
                    noteId, content.Length);
                return Ok(new { content });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving note with ID: {NoteId}", id);
                return StatusCode(500, new { 
                    error = "Error retrieving note", 
                    message = ex.Message,
                    innerError = ex.InnerException?.Message 
                });
            }
        }

        // POST: api/notes/{id}/viewed
        [HttpPost("{id}/viewed")]
        public async Task<IActionResult> MarkNoteAsViewed(string id)
        {
            try
            {
                _logger.LogInformation("Received request to mark note as viewed: {NoteId}", id);
                
                if (!Guid.TryParse(id, out Guid noteId))
                {
                    _logger.LogWarning("Invalid note ID format: {NoteId}", id);
                    return BadRequest("Invalid note ID format.");
                }

                bool success = await _noteService.MarkNoteAsViewedAsync(noteId);
                
                if (!success)
                {
                    _logger.LogWarning("Failed to mark note as viewed, may not exist: {NoteId}", id);
                    return NotFound("Note not found or could not be marked as viewed.");
                }

                _logger.LogInformation("Successfully marked note as viewed: {NoteId}", noteId);
                return Ok(new { success = true, message = "Note marked as viewed and deleted." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking note as viewed: {NoteId}", id);
                return StatusCode(500, new { 
                    error = "Error marking note as viewed", 
                    message = ex.Message,
                    innerError = ex.InnerException?.Message 
                });
            }
        }
    }

    public class CreateNoteDto
    {
        public string Content { get; set; } = string.Empty;
    }
} 