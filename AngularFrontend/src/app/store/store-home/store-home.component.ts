import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { StoreApiService, BrowseBook } from '../store-api.service';

@Component({
  selector: 'app-store-home',
  templateUrl: './store-home.component.html',
  styleUrls: ['./store-home.component.css']
})
export class StoreHomeComponent implements OnInit {

  books: BrowseBook[] = [];
  loading = false;
  error: string | null = null;

  pageNumber = 1;
  pageSize = 10;

  constructor(
    private storeApi: StoreApiService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadBooks();
  }

  loadBooks(): void {
    this.loading = true;
    this.error = null;

    this.storeApi.getBooks(this.pageNumber, this.pageSize).subscribe({
      next: (books) => {
        this.loading = false;
        this.books = books;
      },
      error: (err) => {
        this.loading = false;
        console.error('Failed to load books:', err);
        this.error = 'Could not load books. Please try again.';
      }
    });
  }

  nextPage(): void {
    this.pageNumber++;
    this.loadBooks();
  }

  prevPage(): void {
    if (this.pageNumber > 1) {
      this.pageNumber--;
      this.loadBooks();
    }
  }

  viewSellers(book: BrowseBook): void {
    this.router.navigate(['/store/buy', book.id]);
  }
}
