import { HttpErrorResponse } from '@angular/common/http';
import { Component, ElementRef, OnInit, inject, input, output, signal, viewChild } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Captcha } from '../../models/comment.model';
import { CommentsService } from '../../services/comments.service';
import { checkFile } from '../../validators/file.validator';
import { htmlTextValidator } from '../../validators/html-text.validator';

type FieldName = 'userName' | 'email' | 'homePage' | 'text' | 'captchaCode';

const PATTERN_MESSAGES: Record<FieldName, string> = {
  userName: 'Только латинские буквы и цифры.',
  email: 'Некорректный e-mail.',
  homePage: 'Адрес должен начинаться с http:// или https://.',
  text: 'Некорректный текст.',
  captchaCode: 'Только латинские буквы и цифры.'
};

@Component({
  selector: 'app-comment-form',
  imports: [ReactiveFormsModule],
  templateUrl: './comment-form.html',
  styleUrl: './comment-form.css'
})
export class CommentForm implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly commentsService = inject(CommentsService);

  // id of the comment we reply to, null for a new top-level comment
  readonly parentId = input<number | null>(null);

  readonly created = output<void>();
  readonly cancelled = output<void>();

  private readonly fileInput = viewChild.required<ElementRef<HTMLInputElement>>('fileInput');

  protected readonly form = this.fb.nonNullable.group({
    userName: ['', [Validators.required, Validators.maxLength(50), Validators.pattern(/^[a-zA-Z0-9]+$/)]],
    email: ['', [Validators.required, Validators.email, Validators.maxLength(254)]],
    homePage: ['', [Validators.maxLength(2048), Validators.pattern(/^https?:\/\/\S+$/)]],
    text: ['', [Validators.required, Validators.maxLength(5000), htmlTextValidator]],
    captchaCode: ['', [Validators.required, Validators.maxLength(10), Validators.pattern(/^[a-zA-Z0-9]+$/)]]
  });

  protected readonly captcha = signal<Captcha | null>(null);
  protected readonly file = signal<File | null>(null);
  protected readonly fileError = signal<string | null>(null);
  protected readonly submitting = signal(false);
  protected readonly message = signal<string | null>(null);
  protected readonly serverError = signal<string | null>(null);

  ngOnInit(): void {
    this.loadCaptcha();
  }

  protected loadCaptcha(): void {
    this.commentsService.getCaptcha().subscribe({
      next: captcha => {
        this.captcha.set(captcha);
        this.form.controls.captchaCode.reset();
      },
      error: () => this.serverError.set('Не удалось загрузить CAPTCHA. Попробуйте обновить картинку.')
    });
  }

  protected showError(name: FieldName): boolean {
    const control = this.form.controls[name];
    return control.invalid && (control.touched || control.dirty);
  }

  protected errorText(name: FieldName): string {
    const errors = this.form.controls[name].errors;
    if (!errors) {
      return '';
    }
    if (errors['server']) {
      return errors['server'];
    }
    if (errors['required']) {
      return 'Обязательное поле.';
    }
    if (errors['email']) {
      return 'Некорректный e-mail.';
    }
    if (errors['htmlText']) {
      return errors['htmlText'];
    }
    if (errors['maxlength']) {
      return `Не больше ${errors['maxlength'].requiredLength} символов.`;
    }
    if (errors['pattern']) {
      return PATTERN_MESSAGES[name];
    }
    return 'Некорректное значение.';
  }

  protected onFileChange(event: Event): void {
    const selected = (event.target as HTMLInputElement).files?.[0] ?? null;

    this.file.set(selected);
    this.fileError.set(selected ? checkFile(selected) : null);
  }

  protected submit(): void {
    this.message.set(null);
    this.serverError.set(null);

    this.form.markAllAsTouched();
    if (this.form.invalid || this.fileError() || !this.captcha()) {
      return;
    }

    const value = this.form.getRawValue();
    const data = new FormData();
    data.append('userName', value.userName);
    data.append('email', value.email);
    if (value.homePage) {
      data.append('homePage', value.homePage);
    }
    data.append('text', value.text);
    data.append('captchaId', this.captcha()!.id);
    data.append('captchaCode', value.captchaCode);

    const parentId = this.parentId();
    if (parentId !== null) {
      data.append('parentId', String(parentId));
    }

    const file = this.file();
    if (file) {
      data.append('file', file);
    }

    this.submitting.set(true);
    this.commentsService.createComment(data).subscribe({
      next: () => {
        this.submitting.set(false);
        this.resetForm();
        this.message.set('Комментарий добавлен.');
        this.created.emit();
      },
      error: (error: HttpErrorResponse) => {
        this.submitting.set(false);
        this.showServerErrors(error);
        // a captcha can be checked only once, so a new one is needed after every attempt
        this.loadCaptcha();
      }
    });
  }

  private showServerErrors(error: HttpErrorResponse): void {
    const errors: Record<string, string[]> | undefined = error.error?.errors;
    if (error.status !== 400 || !errors) {
      this.serverError.set('Не удалось отправить комментарий. Попробуйте позже.');
      return;
    }

    for (const [key, messages] of Object.entries(errors)) {
      // the server names the fields like UserName, the form controls are named like userName
      const name = key.charAt(0).toLowerCase() + key.slice(1);
      const control = this.form.get(name);

      if (control) {
        control.setErrors({ server: messages[0] });
        control.markAsTouched();
      } else if (name === 'file') {
        this.fileError.set(messages[0]);
      } else {
        this.serverError.set(messages[0]);
      }
    }
  }

  private resetForm(): void {
    this.form.reset();
    this.file.set(null);
    this.fileError.set(null);
    this.fileInput().nativeElement.value = '';
    this.loadCaptcha();
  }
}
