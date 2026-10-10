import { Component } from '@angular/core';
import { CommentForm } from './components/comment-form/comment-form';

@Component({
  imports: [CommentForm],
  selector: 'app-root',
  styleUrl: './app.css',
  templateUrl: './app.html',
})
export class App {
}
