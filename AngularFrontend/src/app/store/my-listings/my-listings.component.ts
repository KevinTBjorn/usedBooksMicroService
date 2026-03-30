import { Component, OnInit } from '@angular/core';
import { StoreApiService, UserBookListing } from '../store-api.service';

@Component({
  selector: 'app-my-listings',
  templateUrl: './my-listings.component.html',
  styleUrls: ['./my-listings.component.css']
})
export class MyListingsComponent implements OnInit {

  listings: UserBookListing[] = [];
  loading = false;
  error: string | null = null;

  // Later: replace with real user id from auth
  private readonly demoUserId = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';

  constructor(private storeApi: StoreApiService) {}

  ngOnInit(): void {
    this.loadMyListings();
  }

  loadMyListings(): void {
    this.loading = true;
    this.error = null;

    this.storeApi.getUserBooksByUserId(this.demoUserId).subscribe({
      next: (list) => {
        this.loading = false;
        this.listings = list;
      },
      error: (err) => {
        this.loading = false;
        console.error('Failed to load my listings:', err);
        this.error = 'Could not load your listings.';
      }
    });
  }
}
