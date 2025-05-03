import React from 'react';
import { BrowserRouter, Routes, Route, Link } from 'react-router-dom';
import './App.css';
import NoteForm from './components/NoteForm';
import NoteDisplay from './components/NoteDisplay';

function App() {
  return (
    <div className="App">
      <header className="App-header">
        <h1>Private Notes</h1>
        <p>Create self-destructing notes that can only be viewed once</p>
      </header>
      <main className="App-main">
        <BrowserRouter>
          <div style={{ marginBottom: '20px' }}>
            <Link to="/">Home</Link>
          </div>
          <Routes>
            <Route path="/" element={<NoteForm />} />
            <Route path="/note/:id" element={<NoteDisplay />} />
          </Routes>
        </BrowserRouter>
      </main>
      <footer className="App-footer">
        <p>Notes will be destroyed after being read once</p>
      </footer>
    </div>
  );
}

export default App; 