import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthApiService } from '../../auth-api.service';
import { StoreApiService, BookPreview, CreateListingRequest } from '../store-api.service';

@Component({
  selector: 'app-sell',
  templateUrl: './sell.component.html',
  styleUrls: ['./sell.component.css']
})
export class SellComponent implements OnInit {

  isbnForm: FormGroup;
  listingForm: FormGroup;

  preview: BookPreview | null = null;
  previewConfirmed = false;

  loadingPreview = false;
  creatingListing = false;

  successMessage = '';
  errorMessage = '';

  showRecap = false;
  recapData: any = null;

  constructor(
    private fb: FormBuilder,
    private authApi: AuthApiService,
    private storeApi: StoreApiService,
    private router: Router
  ) {
    this.isbnForm = this.fb.group({
      isbn: ['', Validators.required]
    });

    this.listingForm = this.fb.group({
      condition: ['New', Validators.required],
      price: [0, [Validators.required, Validators.min(0)]],
      quantity: [1, [Validators.required, Validators.min(1)]]
    });
  }

  ngOnInit(): void { }

  get isMember(): boolean {
    return this.authApi.isMember();
  }

  get isAdminOrEmployee(): boolean {
    return this.authApi.isAdmin() || this.authApi.isEmployee();
  }

  lookupBook(): void {
    if (this.isbnForm.invalid) return;

    const isbn = this.isbnForm.value.isbn;
    this.loadingPreview = true;
    this.errorMessage = '';

    this.storeApi.previewBook(isbn).subscribe({
      next: (preview) => {
        this.loadingPreview = false;
        this.preview = preview;
        this.previewConfirmed = false;
      },
      error: (err) => {
        this.loadingPreview = false;
        console.error('Preview failed:', err);
        this.errorMessage = 'Could not find book with that ISBN.';
      }
    });
  }

  confirmPreview(): void {
    this.previewConfirmed = true;
  }

  resetPreview(): void {
    this.preview = null;
    this.previewConfirmed = false;
    this.isbnForm.reset();
  }

  createListing(): void {
    if (!this.preview || this.listingForm.invalid) return;

    const req: CreateListingRequest = {
      isbn: this.preview.isbn,
      condition: this.listingForm.value.condition,
      price: this.listingForm.value.price,
      quantity: this.listingForm.value.quantity
    };

    this.creatingListing = true;
    this.errorMessage = '';

    this.storeApi.createListing(req).subscribe({
      next: (result) => {
        this.creatingListing = false;
        this.successMessage = 'Listing created successfully!';

        this.recapData = {
          ...result,
          condition: req.condition,
          price: req.price,
          quantity: req.quantity
        };
        this.showRecap = true;

        setTimeout(() => {
          this.router.navigate(['/store/my-listings']);
        }, 3000);
      },
      error: (err) => {
        this.creatingListing = false;
        console.error('Create listing failed:', err);
        this.errorMessage = 'Failed to create listing.';
      }
    });
  }
}
