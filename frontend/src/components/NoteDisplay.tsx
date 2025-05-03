import React, { useEffect, useState } from 'react';
import { useParams, Link, useNavigate } from 'react-router-dom';

const API_URL = process.env.REACT_APP_API_URL || 'http://localhost:5137';

interface NoteDisplayState {
  content: string | null;
  status: 'loading' | 'loaded' | 'notfound' | 'error' | 'destroyed';
  error: string | null;
  copied: boolean;
}

const NoteDisplay: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [state, setState] = useState<NoteDisplayState>({
    content: null,
    status: 'loading',
    error: null,
    copied: false
  });

  // Mark the note as viewed when leaving the component or when explicitly destroyed
  useEffect(() => {
    return () => {
      // Cleanup function that runs when component unmounts
      if (id && state.status === 'loaded') {
        console.log('Component unmounting, marking note as viewed:', id);
        markNoteAsViewed(id);
      }
    };
  }, [id, state.status]);

  // Function to mark a note as viewed/destroyed
  const markNoteAsViewed = async (noteId: string) => {
    try {
      console.log(`Marking note as viewed: ${noteId}`);
      const response = await fetch(`${API_URL}/api/notes/${noteId}/viewed`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json'
        }
      });
      
      console.log('Mark as viewed response:', response.status);
      if (response.ok) {
        console.log('Successfully marked note as viewed and destroyed');
      } else {
        console.error('Failed to mark note as viewed:', response.status);
      }
    } catch (err) {
      console.error('Error marking note as viewed:', err);
    }
  };

  // Handle explicit destruction by user
  const handleDestroyNote = async () => {
    if (!id) return;
    
    try {
      await markNoteAsViewed(id);
      setState(prev => ({ ...prev, status: 'destroyed' }));
    } catch (err) {
      console.error('Error destroying note:', err);
    }
  };

  // Handle creating a new note
  const handleCreateNewNote = () => {
    navigate('/');
  };

  // Handle copy to clipboard
  const copyToClipboard = (text: string) => {
    navigator.clipboard.writeText(text)
      .then(() => {
        console.log('Text copied to clipboard');
        setState(prev => ({ ...prev, copied: true }));
        
        // Reset copied status after 2 seconds
        setTimeout(() => {
          setState(prev => ({ ...prev, copied: false }));
        }, 2000);
      })
      .catch(err => {
        console.error('Error copying to clipboard:', err);
      });
  };

  useEffect(() => {
    const fetchNote = async () => {
      if (!id) {
        console.error('No note ID found in URL params');
        setState({ content: null, status: 'notfound', error: null, copied: false });
        return;
      }

      console.log(`Attempting to fetch note with ID: ${id}`);
      console.log(`Using API URL: ${API_URL}`);

      try {
        // First try direct API fetch with clean approach
        console.log(`Making fetch request to: ${API_URL}/api/notes/${id}`);
        
        const response = await fetch(`${API_URL}/api/notes/${id}`, {
          method: 'GET'
        });
        
        console.log('API response status:', response.status);
        
        // Get text response first to inspect
        const responseText = await response.text();
        console.log('Response text:', responseText);
        
        if (response.status === 404) {
          console.log('Note not found (404)');
          setState({ content: null, status: 'notfound', error: null, copied: false });
          return;
        }
        
        if (!response.ok) {
          throw new Error(`Error ${response.status}: ${response.statusText}`);
        }
        
        // Try to parse JSON
        try {
          const data = JSON.parse(responseText);
          console.log('Parsed response data:', data);
          
          if (data.content) {
            console.log('Successfully retrieved note content');
            setState({ content: data.content, status: 'loaded', error: null, copied: false });
          } else {
            console.log('Response does not contain note content:', data);
            setState({ content: null, status: 'notfound', error: 'Response did not contain note content', copied: false });
          }
        } catch (parseError) {
          console.error('Failed to parse response as JSON:', parseError);
          setState({ content: null, status: 'error', error: 'Invalid response format', copied: false });
        }
      } catch (err) {
        console.error('Error fetching note:', err);
        const errorMessage = err instanceof Error ? err.message : 'Unknown error';
        setState({ 
          content: null, 
          status: 'error', 
          error: `Failed to retrieve the note: ${errorMessage}`,
          copied: false
        });
      }
    };

    fetchNote();
  }, [id]);

  if (state.status === 'loading') {
    return (
      <div className="note-loading">
        <h2>Loading note...</h2>
        <p>Please wait while we retrieve your note.</p>
      </div>
    );
  }

  if (state.status === 'error') {
    return (
      <div className="note-error">
        <h2>Error Retrieving Note</h2>
        <p>{state.error}</p>
        <p>
          <Link to="/">Return to home page</Link>
        </p>
      </div>
    );
  }

  if (state.status === 'notfound') {
    return (
      <div className="note-not-found">
        <h2>Note Not Found</h2>
        <p>This note does not exist or has already been viewed.</p>
        <p>Secret notes can only be viewed once and are then destroyed.</p>
        <p>
          <button 
            onClick={handleCreateNewNote}
            style={{ 
              padding: '8px 16px',
              background: '#4CAF50',
              color: 'white',
              border: 'none',
              borderRadius: '4px',
              cursor: 'pointer',
              fontWeight: 'bold'
            }}
          >
            Create a new note
          </button>
        </p>
      </div>
    );
  }

  if (state.status === 'destroyed') {
    return (
      <div className="note-destroyed">
        <h2>Note Destroyed</h2>
        <p><strong>This note has been destroyed.</strong></p>
        <p>It can no longer be accessed by anyone.</p>
        <p>
          <button 
            onClick={handleCreateNewNote}
            style={{ 
              padding: '8px 16px',
              background: '#4CAF50',
              color: 'white',
              border: 'none',
              borderRadius: '4px',
              cursor: 'pointer',
              fontWeight: 'bold'
            }}
          >
            Create a new note
          </button>
        </p>
      </div>
    );
  }

  // state.status === 'loaded'
  const noteUrl = window.location.href;
  
  return (
    <div className="note-display">
      <h2>Secure Note</h2>
      
      {/* URL Display and Copy Button */}
      <div style={{ 
        marginBottom: '20px',
        display: 'flex',
        flexDirection: 'column',
        gap: '10px'
      }}>
        <p><strong>Share this secret note:</strong></p>
        <div style={{ 
          display: 'flex',
          alignItems: 'center',
          gap: '10px'
        }}>
          <input
            type="text"
            value={noteUrl}
            readOnly
            onClick={() => copyToClipboard(noteUrl)}
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
            onClick={() => copyToClipboard(noteUrl)}
            style={{
              padding: '10px 16px',
              background: state.copied ? '#4CAF50' : '#2196F3',
              color: 'white',
              border: 'none',
              borderRadius: '4px',
              cursor: 'pointer',
              fontWeight: 'bold',
              minWidth: '120px'
            }}
          >
            {state.copied ? '✓ Copied!' : 'Copy Link'}
          </button>
        </div>
      </div>
      
      {/* Note Content */}
      <div className="note-content" style={{ 
        background: '#f9f9f9', 
        padding: '20px',
        borderRadius: '4px',
        border: '1px solid #ddd',
        marginBottom: '20px'
      }}>
        {state.content}
      </div>
      
      {/* Action Buttons */}
      <div className="note-actions">
        <p><strong>Warning:</strong> This note will be destroyed once you leave this page.</p>
        <p>Click the button below when you're done reading.</p>
        <div style={{ display: 'flex', gap: '10px' }}>
          <button 
            onClick={handleDestroyNote}
            style={{ 
              padding: '8px 16px',
              background: '#f44336',
              color: 'white',
              border: 'none',
              borderRadius: '4px',
              cursor: 'pointer',
              fontWeight: 'bold'
            }}
          >
            I've read this note, destroy it now
          </button>
          <button 
            onClick={handleCreateNewNote}
            style={{ 
              padding: '8px 16px',
              background: '#4CAF50',
              color: 'white',
              border: 'none',
              borderRadius: '4px',
              cursor: 'pointer',
              fontWeight: 'bold'
            }}
          >
            Create a new note
          </button>
        </div>
      </div>
    </div>
  );
};

export default NoteDisplay;