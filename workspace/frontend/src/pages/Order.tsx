import React, { useState } from 'react';
import { TextField, Button, Box, Typography } from '@mui/material';

const Order: React.FC = () => {
  const [bookId, setBookId] = useState('');
  const [quantity, setQuantity] = useState(1);
  const [status, setStatus] = useState('');

  const handleOrder = async (e: React.FormEvent) => {
    e.preventDefault();
    const response = await fetch('/order', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ bookId, quantity }),
    });
    if (response.ok) {
      setStatus('Order placed successfully!');
    } else {
      setStatus('Failed to place order.');
    }
  };

  return (
    <Box sx={{ maxWidth: 400, mx: 'auto', mt: 4 }}>
      <Typography variant="h5" gutterBottom>Order Book</Typography>
      <form onSubmit={handleOrder}>
        <TextField label="Book ID" fullWidth margin="normal" value={bookId} onChange={e => setBookId(e.target.value)} />
        <TextField label="Quantity" type="number" fullWidth margin="normal" value={quantity} onChange={e => setQuantity(Number(e.target.value))} />
        <Button type="submit" variant="contained" color="primary" fullWidth>Place Order</Button>
      </form>
      {status && <Typography color="secondary" sx={{ mt: 2 }}>{status}</Typography>}
    </Box>
  );
};

export default Order;
