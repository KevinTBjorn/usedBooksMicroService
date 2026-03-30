import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { StoreApiService, UserBookListing } from '../store-api.service';

@Component({
  selector: 'app-buy',
  templateUrl: './buy.component.html',
  styleUrls: ['./buy.component.css']
})
export class BuyComponent implements OnInit {

  bookId: string | null = null;
  listings: UserBookListing[] = [];
  loading = false;
  error: string | null = null;

  constructor(
    private route: ActivatedRoute,
    private storeApi: StoreApiService
  ) {}

  ngOnInit(): void {
    this.bookId = this.route.snapshot.paramMap.get('bookId');
    if (!this.bookId) {
      // No book selected – show a friendly message in template
      return;
    }

    this.loadListings(this.bookId);
  }

  loadListings(bookId: string): void {
    this.loading = true;
    this.error = null;

    this.storeApi.getUserBooksByBookId(bookId).subscribe({
      next: (list) => {
        this.loading = false;
        this.listings = list;
      },
      error: (err) => {
        this.loading = false;
        console.error('Failed to load userbooks:', err);
        this.error = 'Could not load sellers for this book.';
      }
    });
  }
}
