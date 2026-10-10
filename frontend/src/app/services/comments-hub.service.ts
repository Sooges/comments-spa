import { Injectable } from '@angular/core';
import { HubConnectionBuilder } from '@microsoft/signalr';
import { Observable, Subject } from 'rxjs';

// Gets the messages of the server over a WebSocket (SignalR):
// the server tells that somebody added a comment, so the list can be updated without reloading the page.
@Injectable({ providedIn: 'root' })
export class CommentsHubService {
  private readonly connection = new HubConnectionBuilder()
    .withUrl('/hubs/comments')
    .withAutomaticReconnect()
    .build();

  private readonly commentAdded = new Subject<void>();

  // it emits every time when the list may have changed
  readonly commentAdded$: Observable<void> = this.commentAdded.asObservable();

  constructor() {
    this.connection.on('CommentAdded', () => this.commentAdded.next());

    // some messages may be lost while the connection was broken, so the list is loaded again
    this.connection.onreconnected(() => this.commentAdded.next());
  }

  start(): void {
    this.connection.start().catch(error => console.error('Could not connect to the comments hub', error));
  }
}
