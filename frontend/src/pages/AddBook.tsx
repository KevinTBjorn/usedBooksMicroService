import React, { useState } from 'react';
import { TextField, Button, Box, Typography } from '@mui/material';

const AddBook: React.FC = () => {
  const [title, setTitle] = useState('');
  const [author, setAuthor] = useState('');
  const [isbn, setIsbn] = useState('');
  const [stock, setStock] = useState(0);
  const [status, setStatus] = useState('');

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    const response = await fetch('/addbook', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ title, author, isbn, stock }),
    });
    if (response.ok) {
      setStatus('Book added successfully!');
    } else {
      setStatus('Failed to add book.');
    }
  };

  return (
    <Box sx={{ maxWidth: 400, mx: 'auto', mt: 4 }}>
      <Typography variant="h5" gutterBottom>Add Book</Typography>
      <form onSubmit={handleSubmit}>
        <TextField label="Title" fullWidth margin="normal" value={title} onChange={e => setTitle(e.target.value)} />
        <TextField label="Author" fullWidth margin="normal" value={author} onChange={e => setAuthor(e.target.value)} />
        <TextField label="ISBN" fullWidth margin="normal" value={isbn} onChange={e => setIsbn(e.target.value)} />
        <TextField label="Stock" type="number" fullWidth margin="normal" value={stock} onChange={e => setStock(Number(e.target.value))} />
        <Button type="submit" variant="contained" color="primary" fullWidth>Add Book</Button>
      </form>
      {status && <Typography color="secondary" sx={{ mt: 2 }}>{status}</Typography>}
    </Box>
  );
};

export default AddBook;
