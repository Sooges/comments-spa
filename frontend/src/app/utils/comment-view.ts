// the first letters of the name, they are shown in the avatar
export function getInitials(name: string): string {
  return name.slice(0, 2).toUpperCase();
}

// the same name always gets the same color
export function getAvatarColor(name: string): string {
  let hue = 0;
  for (const char of name) {
    hue = (hue * 31 + char.charCodeAt(0)) % 360;
  }
  return `hsl(${hue}, 55%, 60%)`;
}

// turns the HTML of a comment into a short plain text for the quote in a reply
export function toQuote(html: string, maxLength = 80): string {
  const text = new DOMParser().parseFromString(html, 'text/html').body.textContent ?? '';
  const oneLine = text.replace(/\s+/g, ' ').trim();
  return oneLine.length > maxLength ? oneLine.slice(0, maxLength) + '...' : oneLine;
}
