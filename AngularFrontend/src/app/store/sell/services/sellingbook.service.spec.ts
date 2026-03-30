import { TestBed } from '@angular/core/testing';

import { SellingbookService } from './sellingbook.service';

describe('SellingbookService', () => {
  let service: SellingbookService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(SellingbookService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
