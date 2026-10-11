import { Injectable } from '@angular/core';
import { HubConnectionBuilder, HubConnectionState } from '@microsoft/signalr';
import { Observable, Subject } from 'rxjs';

// how long to wait before the next try to connect
const RETRY_DELAY_MS = 5000;
const MAX_RECONNECT_DELAY_MS = 30000;

// Gets the messages of the server over a WebSocket (SignalR):
// the server tells that somebody added a comment, so the list can be updated without reloading the page.
@Injectable({ providedIn: 'root' })
export class CommentsHubService {
  private readonly connection = new HubConnectionBuilder()
    .withUrl('/hubs/comments')
    // By default the connection gives up after about a minute without the server. Here it tries again
    // and again, with a longer pause every time (but not longer than 30 seconds).
    .withAutomaticReconnect({
      nextRetryDelayInMilliseconds: context =>
        Math.min(MAX_RECONNECT_DELAY_MS, (context.previousRetryCount + 1) * 2000)
    })
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
    this.connect(false);
  }

  private connect(afterFailure: boolean): void {
    // the connection is already working or is being made
    if (this.connection.state !== HubConnectionState.Disconnected) {
      return;
    }

    this.connection.start()
      .then(() => {
        // the first try did not work, so some messages could be lost
        if (afterFailure) {
          this.commentAdded.next();
        }
      })
      .catch(error => {
        console.error('Could not connect to the comments hub, trying again', error);
        setTimeout(() => this.connect(true), RETRY_DELAY_MS);
      });
  }
}
