import React from 'react';
import { BrowserRouter as Router, Routes, Route, Link } from 'react-router-dom';
import { AppBar, Toolbar, Button, Container } from '@mui/material';
import AddBook from './pages/AddBook';
import Search from './pages/Search';
import Order from './pages/Order';
import Warehouse from './pages/Warehouse';
import './App.css';

function App() {
  return (
    <Router>
      <AppBar position="static">
        <Toolbar>
          <Button color="inherit" component={Link} to="/addbook">Add Book</Button>
          <Button color="inherit" component={Link} to="/search">Search</Button>
          <Button color="inherit" component={Link} to="/order">Order</Button>
          <Button color="inherit" component={Link} to="/warehouse">Warehouse</Button>
        </Toolbar>
      </AppBar>
      <Container>
        <Routes>
          <Route path="/addbook" element={<AddBook />} />
          <Route path="/search" element={<Search />} />
          <Route path="/order" element={<Order />} />
          <Route path="/warehouse" element={<Warehouse />} />
          <Route path="/" element={<Search />} />
        </Routes>
      </Container>
    </Router>
  );
}

export default App;
