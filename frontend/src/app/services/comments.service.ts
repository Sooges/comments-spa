import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { Captcha, Comment, PagedResult, SortField, SortOrder } from '../models/comment.model';

@Injectable({ providedIn: 'root' })
export class CommentsService {
  private readonly http = inject(HttpClient);

  getComments(sortBy: SortField, sortOrder: SortOrder, page: number): Observable<PagedResult<Comment>> {
    const params = new HttpParams()
      .set('sortBy', sortBy)
      .set('sortOrder', sortOrder)
      .set('page', page);

    return this.http.get<PagedResult<Comment>>('/api/comments', { params });
  }

  // the comment is sent as a form, because it can contain a file
  createComment(formData: FormData): Observable<Comment> {
    return this.http.post<Comment>('/api/comments', formData);
  }

  preview(text: string): Observable<{ html: string }> {
    return this.http.post<{ html: string }>('/api/comments/preview', { text });
  }

  getCaptcha(): Observable<Captcha> {
    return this.http.get<Captcha>('/api/captcha');
  }
}
