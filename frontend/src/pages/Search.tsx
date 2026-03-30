import React, { useState } from 'react';
import { TextField, Button, Box, Typography, List, ListItem, ListItemText } from '@mui/material';

const Search: React.FC = () => {
  const [query, setQuery] = useState('');
  const [results, setResults] = useState<any[]>([]);

  const handleSearch = async () => {
    const response = await fetch(`/search?title=${encodeURIComponent(query)}`);
    if (response.ok) {
      const data = await response.json();
      setResults(data);
    } else {
      setResults([]);
    }
  };

  return (
    <Box sx={{ maxWidth: 400, mx: 'auto', mt: 4 }}>
      <Typography variant="h5" gutterBottom>Search Books</Typography>
      <TextField label="Title" fullWidth margin="normal" value={query} onChange={e => setQuery(e.target.value)} />
      <Button variant="contained" color="primary" fullWidth onClick={handleSearch}>Search</Button>
      <List>
        {results.map((book, idx) => (
          <ListItem key={idx}>
            <ListItemText primary={`${book.title} by ${book.author}`} secondary={`ISBN: ${book.isbn}, Stock: ${book.stock}`} />
          </ListItem>
        ))}
      </List>
    </Box>
  );
};

export default Search;
