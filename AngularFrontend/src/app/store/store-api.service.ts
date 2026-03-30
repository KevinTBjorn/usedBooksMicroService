import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

// ===== SELL FLOW TYPES =====
export interface BookPreview {
  id: string;
  title: string;
  author: string;
  isbn: string;
  description: string;
  imageUrl: string;
  genre: string;
}

export interface CreateListingRequest {
  isbn: string;
  condition: string;
  price: number;
  quantity: number;
}

// ===== BROWSE / WAREHOUSE TYPES =====
export interface BrowseBook {
  id: string;
  title: string;
  isbn: string;
  description: string;
  edition: string;
  year: number;
  author: string;
  imageUrl: string;
  genre: string;
}

export interface UserBookListing {
  bookId: string;
  userId: string;
  condition: string;
  quantity: number;
  price: number;
}

@Injectable({
  providedIn: 'root'
})
export class StoreApiService {

  private readonly baseUrl = 'http://localhost:5139';

  constructor(private http: HttpClient) {}

  // ==============================
  //  SELL FLOW (AddBook)
  // ==============================

  previewBook(isbn: string): Observable<BookPreview> {
    return this.http.post<BookPreview>(
      `${this.baseUrl}/store/listings/preview`,
      { isbn }
    );
  }

  createListing(request: CreateListingRequest): Observable<BookPreview> {
    return this.http.post<BookPreview>(
      `${this.baseUrl}/store/listings`,
      request,
      { withCredentials: true }
    );

  }

  // ==============================
  //  STORE / WAREHOUSE ENDPOINTS
  // ==============================

  getBooks(pageNumber: number, pageSize: number): Observable<BrowseBook[]> {
    const params = new HttpParams()
      .set('pageNumber', pageNumber)
      .set('pageSize', pageSize);

    return this.http.get<BrowseBook[]>(`${this.baseUrl}/store/books`, { params });
  }

  getUserBooksByBookId(bookId: string): Observable<UserBookListing[]> {
    return this.http.get<UserBookListing[]>(
      `${this.baseUrl}/store/userbooks/${bookId}`
    );
  }

  getUserBooksByUserId(userId: string): Observable<UserBookListing[]> {
    return this.http.get<UserBookListing[]>(
      `${this.baseUrl}/store/userbooks/user/${userId}`
    );
  }
}
