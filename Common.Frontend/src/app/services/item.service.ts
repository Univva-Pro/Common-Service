import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AuthService } from './auth.service';
import { CommonItemResponse, CommonItemRequest } from '../models/item.model';

@Injectable({
  providedIn: 'root'
})
export class ItemService {
  private apiUrl = '/api/common/items';

  constructor(private http: HttpClient, private authService: AuthService) { }

  private getHeaders() {
    return new HttpHeaders({
      'Authorization': `Bearer ${this.authService.getToken()}`
    });
  }

  getItems(): Observable<any[]> {
    return this.http.get<any[]>(this.apiUrl, { headers: this.getHeaders() });
  }

  addItem(item: CommonItemRequest): Observable<any> {
    return this.http.post(this.apiUrl, item, { headers: this.getHeaders() });
  }

  deleteItem(id: string): Observable<any> {
    return this.http.delete(`${this.apiUrl}/${id}`, { headers: this.getHeaders() });
  }

  updateItem(id: string, item: CommonItemRequest): Observable<any> {
    return this.http.put(`${this.apiUrl}/${id}`, item, { headers: this.getHeaders() });
  }
}
