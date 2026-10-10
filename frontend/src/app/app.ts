import { Component, viewChild } from '@angular/core';
import { CommentForm } from './components/comment-form/comment-form';
import { CommentList } from './components/comment-list/comment-list';
import { Lightbox } from './components/lightbox/lightbox';

@Component({
  imports: [CommentForm, CommentList, Lightbox],
  selector: 'app-root',
  styleUrl: './app.css',
  templateUrl: './app.html',
})
export class App {
  private readonly list = viewChild.required(CommentList);

  // a new comment is shown at the top of the first page
  protected onCommentCreated(): void {
    this.list().reload(true);
  }
}
