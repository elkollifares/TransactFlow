import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Transaction, TransactionRequest, TransactionSummary, TransactionType } from './models';

@Injectable({ providedIn: 'root' })
export class TransactionService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/transactions';

  list(search = '', type: TransactionType | '' = ''): Observable<Transaction[]> {
    let params = new HttpParams();
    if (search.trim()) params = params.set('search', search.trim());
    if (type) params = params.set('type', type);
    return this.http.get<Transaction[]>(this.baseUrl, { params });
  }

  summary(): Observable<TransactionSummary> {
    return this.http.get<TransactionSummary>(`${this.baseUrl}/summary`);
  }

  create(request: TransactionRequest): Observable<Transaction> {
    return this.http.post<Transaction>(this.baseUrl, request);
  }

  update(id: string, request: TransactionRequest): Observable<Transaction> {
    return this.http.put<Transaction>(`${this.baseUrl}/${id}`, request);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }

  recalculate(): Observable<TransactionSummary> {
    return this.http.post<TransactionSummary>(`${this.baseUrl}/recalculate`, {});
  }
}

