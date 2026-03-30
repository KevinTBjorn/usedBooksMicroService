import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { CreateBookRequest } from '../../../interfaces/CreateBookRequest';

@Injectable({
  providedIn: 'root'
})
export class SellingbookService {

  private readonly apiUrl = 'http://localhost:5030/api/Book/';

  constructor(private http: HttpClient) { }

  createBookListing(listingData: CreateBookRequest) {
    return this.http.post(`${this.apiUrl}`, listingData);
  }
}
