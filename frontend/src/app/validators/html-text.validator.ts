import { AbstractControl, ValidationErrors } from '@angular/forms';

// The same rules as on the server (HtmlTextProcessor): only the tags a, code, i and strong
// are allowed and they must be closed in the right order.

// everything that looks like a tag: <...>
const ANY_TAG = /<[^<>]*>/g;
// </a>, </code>, </i>, </strong>
const CLOSING_TAG = /^<\/\s*(a|code|i|strong)\s*>$/i;
// <code>, <i>, <strong>
const SIMPLE_OPENING_TAG = /^<\s*(code|i|strong)\s*>$/i;
// <a href="..." title="...">, the attributes are in the first group
const LINK_OPENING_TAG = /^<\s*a((?:\s+(?:href|title)\s*=\s*"[^"]*")*)\s*>$/i;
const LINK_ATTRIBUTE = /(href|title)\s*=\s*"([^"]*)"/gi;

const ALLOWED_TAGS_HINT = 'Можно использовать только <a href="" title="">, <code>, <i> и <strong>.';

/**
 * Returns the text of the first problem or null if the text is correct.
 */
export function checkHtmlText(text: string): string | null {
  const openTags: string[] = [];
  let position = 0;

  for (const match of text.matchAll(ANY_TAG)) {
    const start = match.index ?? 0;

    // a "<" that is not a part of an allowed tag
    if (text.substring(position, start).includes('<')) {
      return 'Символ "<" можно использовать только в разрешённых тегах: a, code, i, strong.';
    }
    position = start + match[0].length;

    const error = checkTag(match[0], openTags);
    if (error) {
      return error;
    }
  }

  if (text.substring(position).includes('<')) {
    return 'Символ "<" можно использовать только в разрешённых тегах: a, code, i, strong.';
  }

  if (openTags.length > 0) {
    return `Тег <${openTags[openTags.length - 1]}> не закрыт.`;
  }

  return null;
}

function checkTag(tag: string, openTags: string[]): string | null {
  const closing = CLOSING_TAG.exec(tag);
  if (closing) {
    const name = closing[1].toLowerCase();
    if (openTags[openTags.length - 1] !== name) {
      return `Тег </${name}> закрыт неверно: проверьте порядок и вложенность тегов.`;
    }
    openTags.pop();
    return null;
  }

  const simple = SIMPLE_OPENING_TAG.exec(tag);
  if (simple) {
    openTags.push(simple[1].toLowerCase());
    return null;
  }

  const link = LINK_OPENING_TAG.exec(tag);
  if (link) {
    return checkLink(link[1], openTags);
  }

  const shown = tag.length > 40 ? tag.substring(0, 40) + '...>' : tag;
  return `Тег ${shown} не разрешён. ${ALLOWED_TAGS_HINT}`;
}

function checkLink(attributes: string, openTags: string[]): string | null {
  if (openTags.includes('a')) {
    return 'Ссылки нельзя вкладывать друг в друга.';
  }

  let href: string | null = null;
  for (const attribute of attributes.matchAll(LINK_ATTRIBUTE)) {
    const name = attribute[1].toLowerCase();
    if (name === 'href') {
      href = attribute[2];
    }
  }

  if (!href || !isHttpUrl(href.trim())) {
    return 'У ссылки <a> должен быть корректный адрес в атрибуте href (http:// или https://).';
  }

  openTags.push('a');
  return null;
}

function isHttpUrl(value: string): boolean {
  try {
    const url = new URL(value);
    return url.protocol === 'http:' || url.protocol === 'https:';
  } catch {
    return false;
  }
}

export function htmlTextValidator(control: AbstractControl): ValidationErrors | null {
  const error = checkHtmlText(control.value ?? '');
  return error ? { htmlText: error } : null;
}
