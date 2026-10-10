import { Component, inject } from '@angular/core';
import { LightboxService } from '../../services/lightbox.service';

@Component({
  selector: 'app-lightbox',
  templateUrl: './lightbox.html',
  styleUrl: './lightbox.css',
  host: {
    // the window is closed with the Esc key
    '(document:keydown.escape)': 'lightbox.close()'
  }
})
export class Lightbox {
  protected readonly lightbox = inject(LightboxService);
}
