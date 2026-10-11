import { DatePipe } from '@angular/common';
import { Component, computed, inject, input, output, signal } from '@angular/core';
import { Attachment, Comment } from '../../models/comment.model';
import { LightboxService } from '../../services/lightbox.service';
import { getAvatarColor, getInitials, toQuote } from '../../utils/comment-view';
import { CommentForm } from '../comment-form/comment-form';

// One comment with its attachment, the reply form and all the replies.
// It uses itself to show the replies, so the nesting can be of any depth.
@Component({
  selector: 'app-comment-node',
  imports: [DatePipe, CommentForm],
  templateUrl: './comment-node.html',
  styleUrl: './comment-node.css'
})
export class CommentNode {
  private readonly lightbox = inject(LightboxService);

  readonly comment = input.required<Comment>();

  // the comment this one replies to, it is used for the quote
  readonly parent = input<Comment | null>(null);

  // top-level comments are shown in the table, which has its own header
  readonly showHeader = input(true);

  // how deep the comment is: 0 for a top-level one
  readonly depth = input(0);

  // Every level of replies is moved to the right, but only the first levels. Deeper replies stay
  // on the same line, otherwise a long chain would go out of the screen.
  protected readonly maxIndentLevels = 6;

  // a reply was saved, the list has to be loaded again
  readonly changed = output<void>();

  protected readonly replying = signal(false);

  protected readonly initials = computed(() => getInitials(this.comment().userName));
  protected readonly color = computed(() => getAvatarColor(this.comment().userName));
  protected readonly quote = computed(() => {
    const parent = this.parent();
    return parent ? toQuote(parent.text) : null;
  });

  protected getFileUrl(file: Attachment): string {
    return `/api/attachments/${file.id}`;
  }

  protected openImage(file: Attachment): void {
    this.lightbox.openImage(this.getFileUrl(file), file.fileName);
  }

  protected openText(file: Attachment): void {
    this.lightbox.openText(this.getFileUrl(file), file.fileName);
  }

  protected onReplied(): void {
    this.replying.set(false);
    this.changed.emit();
  }
}
