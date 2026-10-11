import { HttpErrorResponse } from '@angular/common/http';
import { Component, ElementRef, OnInit, inject, input, output, signal, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { debounceTime } from 'rxjs';
import { Captcha } from '../../models/comment.model';
import { CommentsService } from '../../services/comments.service';
import { checkFile } from '../../validators/file.validator';
import { checkHtmlText, htmlTextValidator } from '../../validators/html-text.validator';

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
  // There can be several forms on the page (the main one and the forms for replies),
  // so the ids of the fields must be different, otherwise a click on a label goes to the wrong form.
  private static nextUid = 1;
  protected readonly uid = CommentForm.nextUid++;

  private readonly fb = inject(FormBuilder);
  private readonly commentsService = inject(CommentsService);

  // id of the comment we reply to, null for a new top-level comment
  readonly parentId = input<number | null>(null);

  readonly created = output<void>();
  readonly cancelled = output<void>();

  private readonly fileInput = viewChild.required<ElementRef<HTMLInputElement>>('fileInput');
  private readonly textArea = viewChild.required<ElementRef<HTMLTextAreaElement>>('textArea');

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

  // it is kept apart from the field, because the field is cleared when a new picture is loaded
  protected readonly captchaError = signal<string | null>(null);

  protected readonly previewOpen = signal(false);
  protected readonly previewHtml = signal('');
  protected readonly previewError = signal<string | null>(null);

  constructor() {
    // while the preview is open it follows the text, but not on every key press
    this.form.controls.text.valueChanges
      .pipe(debounceTime(400), takeUntilDestroyed())
      .subscribe(() => {
        if (this.previewOpen()) {
          this.loadPreview();
        }
      });
  }

  ngOnInit(): void {
    this.loadCaptcha();
  }

  protected refreshCaptcha(): void {
    this.captchaError.set(null);
    this.loadCaptcha();
  }

  private loadCaptcha(): void {
    this.commentsService.getCaptcha().subscribe({
      next: captcha => {
        this.captcha.set(captcha);
        this.form.controls.captchaCode.reset();
      },
      error: () => this.serverError.set('Не удалось загрузить CAPTCHA. Попробуйте обновить картинку.')
    });
  }

  // wraps the selected text in a tag, or puts an empty tag at the cursor
  protected insertTag(tag: 'i' | 'strong' | 'code' | 'a'): void {
    const textarea = this.textArea().nativeElement;
    const control = this.form.controls.text;
    const start = textarea.selectionStart;
    const end = textarea.selectionEnd;

    let openTag = `<${tag}>`;
    if (tag === 'a') {
      const href = window.prompt('Адрес ссылки (http:// или https://)', 'https://');
      if (!href) {
        return;
      }
      // quotes and brackets would break the tag
      const safeHref = href.trim().replace(/["<>]/g, char => encodeURIComponent(char));
      openTag = `<a href="${safeHref}" title="">`;
    }

    const value = control.value;
    control.setValue(
      value.substring(0, start) + openTag + value.substring(start, end) + `</${tag}>` + value.substring(end)
    );
    control.markAsDirty();

    // the cursor goes right after the selected text, before the closing tag
    const cursor = start + openTag.length + (end - start);
    textarea.focus();
    textarea.setSelectionRange(cursor, cursor);
  }

  protected togglePreview(): void {
    this.previewOpen.update(open => !open);
    if (this.previewOpen()) {
      this.loadPreview();
    }
  }

  private loadPreview(): void {
    const text = this.form.controls.text.value;

    if (!text.trim()) {
      this.previewHtml.set('');
      this.previewError.set(null);
      return;
    }

    const error = checkHtmlText(text);
    if (error) {
      this.previewError.set(error);
      return;
    }

    // the server builds the same HTML that will be saved, so the preview shows the real result
    this.commentsService.preview(text).subscribe({
      next: response => {
        this.previewError.set(null);
        this.previewHtml.set(response.html);
      },
      error: (error: HttpErrorResponse) =>
        this.previewError.set(error.error?.errors?.Text?.[0] ?? 'Не удалось получить предпросмотр.')
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
    this.captchaError.set(null);

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

      if (name === 'captchaCode' || name === 'captchaId') {
        this.captchaError.set(messages[0]);
      } else if (control) {
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
    this.previewOpen.set(false);
    this.previewHtml.set('');
    this.loadCaptcha();
  }
}
