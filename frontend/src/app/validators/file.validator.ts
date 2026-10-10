const MAX_TEXT_FILE_SIZE = 100 * 1024;
const MAX_IMAGE_FILE_SIZE = 5 * 1024 * 1024;

const IMAGE_EXTENSIONS = ['.jpg', '.jpeg', '.png', '.gif'];

/**
 * Checks a file before it is sent. Big pictures are not rejected here,
 * the server makes them smaller (up to 320x240).
 * Returns the text of the problem or null if the file is fine.
 */
export function checkFile(file: File): string | null {
  const name = file.name.toLowerCase();
  const isText = name.endsWith('.txt');
  const isImage = IMAGE_EXTENSIONS.some(extension => name.endsWith(extension));

  if (!isText && !isImage) {
    return 'Можно прикрепить картинку (JPG, GIF, PNG) или текстовый файл (TXT).';
  }

  if (file.size === 0) {
    return 'Файл пустой.';
  }

  if (isText && file.size > MAX_TEXT_FILE_SIZE) {
    return 'Текстовый файл не должен быть больше 100 КБ.';
  }

  if (isImage && file.size > MAX_IMAGE_FILE_SIZE) {
    return 'Файл с картинкой не должен быть больше 5 МБ.';
  }

  return null;
}
