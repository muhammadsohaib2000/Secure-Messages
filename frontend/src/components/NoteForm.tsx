import React, { useState } from 'react';

const API_URL = process.env.REACT_APP_API_URL || 'http://localhost:5137';

const NoteForm: React.FC = () => {
  const [content, setContent] = useState("");
  const [noteLink, setNoteLink] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [copied, setCopied] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    if (!content.trim()) {
      setError("Note content cannot be empty");
      return;
    }

    setIsLoading(true);
    setError(null);

    console.log(`Posting to API URL: ${API_URL}/api/notes`);
    console.log('Request body:', { content });

    try {
      const response = await fetch(`${API_URL}/api/notes`, {
        method: 'POST',
        mode: 'cors',
        headers: {
          'Content-Type': 'application/json',
          'Accept': 'application/json'
        },
        body: JSON.stringify({ content }),
      });

      console.log('Full response:', response);
      console.log('API response status:', response.status);

      if (!response.ok) {
        const errorText = await response.text();
        console.error('Response error text:', errorText);
        throw new Error(`Failed to create note: ${response.status} ${response.statusText}`);
      }

      const data = await response.json();
      console.log('Response data:', data);
      const noteId = data.noteId;
      console.log(`Note created with ID: ${noteId}`);
      
      const link = `${window.location.origin}/note/${noteId}`;
      console.log(`Generated link: ${link}`);
      
      setNoteLink(link);
      setContent('');
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : 'Unknown error';
      setError(`Error creating note: ${errorMessage}`);
      console.error('Error creating note:', err);
    } finally {
      setIsLoading(false);
    }
  };

  const createNewNote = () => {
    setNoteLink(null);
    setError(null);
    setCopied(false);
  };

  const copyToClipboard = () => {
    if (noteLink) {
      navigator.clipboard.writeText(noteLink)
        .then(() => {
          console.log('Link copied to clipboard');
          setCopied(true);
          setTimeout(() => setCopied(false), 2000);
        })
        .catch(err => {
          console.error('Error copying to clipboard:', err);
          setError('Failed to copy to clipboard');
        });
    }
  };

  return (
    <div className="note-form">
      {noteLink ? (
        <div className="note-created">
          <h2>Your note is ready!</h2>
          <div className="note-link" style={{ marginBottom: '20px' }}>
            <p><strong>Share this link with the recipient:</strong></p>
            <div style={{ 
              display: 'flex',
              alignItems: 'center',
              gap: '10px',
              marginBottom: '10px'
            }}>
              <input 
                type="text" 
                value={noteLink} 
                readOnly 
                onClick={copyToClipboard}
                style={{ 
                  flex: 1,
                  padding: '10px', 
                  borderRadius: '4px',
                  border: '1px solid #ccc',
                  cursor: 'pointer',
                  backgroundColor: '#f8f8f8',
                  fontFamily: 'monospace'
                }}
              />
              <button
                onClick={copyToClipboard}
                style={{
                  padding: '10px 16px',
                  background: copied ? '#4CAF50' : '#2196F3',
                  color: 'white',
                  border: 'none',
                  borderRadius: '4px',
                  cursor: 'pointer',
                  fontWeight: 'bold',
                  minWidth: '120px'
                }}
              >
                {copied ? '✓ Copied!' : 'Copy Link'}
              </button>
            </div>
          </div>
          <button 
            onClick={createNewNote}
            style={{ 
              padding: '8px 16px',
              background: '#4CAF50',
              color: 'white',
              border: 'none',
              borderRadius: '4px',
              cursor: 'pointer',
              fontWeight: 'bold',
              marginTop: '10px'
            }}
          >
            Create Another Note
          </button>
        </div>
      ) : (
        <form onSubmit={handleSubmit}>
          <h2>Create a Private Note</h2>
          {error && <div className="error-message" style={{ color: 'red', margin: '10px 0' }}>{error}</div>}
          <textarea
            value={content}
            onChange={(e) => setContent(e.target.value)}
            placeholder="Type your secret note here..."
            disabled={isLoading}
            required
            style={{
              width: '100%',
              minHeight: '150px',
              padding: '10px',
              border: '1px solid #ccc',
              borderRadius: '4px',
              marginBottom: '15px',
              fontFamily: 'inherit'
            }}
          />
          <button 
            type="submit" 
            disabled={isLoading}
            style={{ 
              padding: '10px 16px',
              background: '#2196F3',
              color: 'white',
              border: 'none',
              borderRadius: '4px',
              cursor: isLoading ? 'not-allowed' : 'pointer',
              fontWeight: 'bold'
            }}
          >
            {isLoading ? 'Creating...' : 'Create Secure Note'}
          </button>
        </form>
      )}
    </div>
  );
};

export default NoteForm; 