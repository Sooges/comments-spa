import { DatePipe } from '@angular/common';
import { Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { debounceTime } from 'rxjs';
import { Comment, PagedResult, SortField, SortOrder } from '../../models/comment.model';
import { CommentsHubService } from '../../services/comments-hub.service';
import { CommentsService } from '../../services/comments.service';
import { getAvatarColor, getInitials } from '../../utils/comment-view';
import { CommentNode } from '../comment-node/comment-node';

// How many page buttons are shown on every side of the current page
const PAGE_BUTTONS_AROUND = 3;

@Component({
  selector: 'app-comment-list',
  imports: [DatePipe, CommentNode],
  templateUrl: './comment-list.html',
  styleUrl: './comment-list.css'
})
export class CommentList implements OnInit {
  private readonly commentsService = inject(CommentsService);
  private readonly hub = inject(CommentsHubService);
  private readonly destroyRef = inject(DestroyRef);

  // the newest comments go first by default (LIFO)
  protected readonly sortBy = signal<SortField>('date');
  protected readonly sortOrder = signal<SortOrder>('desc');
  protected readonly page = signal(1);

  protected readonly result = signal<PagedResult<Comment> | null>(null);
  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);

  protected readonly pageNumbers = computed(() => {
    const result = this.result();
    if (!result) {
      return [];
    }

    const first = Math.max(1, result.page - PAGE_BUTTONS_AROUND);
    const last = Math.min(result.totalPages, result.page + PAGE_BUTTONS_AROUND);
    return Array.from({ length: last - first + 1 }, (_, index) => first + index);
  });

  // only the answer to the last request is used, if the user clicks fast
  private lastRequest = 0;

  ngOnInit(): void {
    this.load();

    // When somebody adds a comment, the server tells it over the WebSocket and the list is loaded again.
    // The delay is for the case when several comments come one after another.
    this.hub.commentAdded$
      .pipe(debounceTime(300), takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.load());
    this.hub.start();
  }

  // loads the list again, for example after a new comment was added
  reload(goToFirstPage = false): void {
    if (goToFirstPage) {
      this.page.set(1);
    }
    this.load();
  }

  protected sort(field: SortField): void {
    if (this.sortBy() === field) {
      this.sortOrder.set(this.sortOrder() === 'asc' ? 'desc' : 'asc');
    } else {
      this.sortBy.set(field);
      // the date starts with the newest comments, the names start from A
      this.sortOrder.set(field === 'date' ? 'desc' : 'asc');
    }

    this.page.set(1);
    this.load();
  }

  protected goToPage(page: number): void {
    const totalPages = this.result()?.totalPages ?? 1;
    if (page < 1 || page > totalPages || page === this.page()) {
      return;
    }

    this.page.set(page);
    this.load();
  }

  protected arrow(field: SortField): string {
    if (this.sortBy() !== field) {
      return '';
    }
    return this.sortOrder() === 'asc' ? '▲' : '▼';
  }

  protected ariaSort(field: SortField): 'ascending' | 'descending' | 'none' {
    if (this.sortBy() !== field) {
      return 'none';
    }
    return this.sortOrder() === 'asc' ? 'ascending' : 'descending';
  }

  protected initials(name: string): string {
    return getInitials(name);
  }

  protected color(name: string): string {
    return getAvatarColor(name);
  }

  private load(): void {
    const request = ++this.lastRequest;
    this.loading.set(true);

    this.commentsService.getComments(this.sortBy(), this.sortOrder(), this.page()).subscribe({
      next: result => {
        if (request !== this.lastRequest) {
          return;
        }
        this.result.set(result);
        this.error.set(null);
        this.loading.set(false);
      },
      error: () => {
        if (request !== this.lastRequest) {
          return;
        }
        this.error.set('Не удалось загрузить комментарии. Попробуйте обновить страницу.');
        this.loading.set(false);
      }
    });
  }
}
