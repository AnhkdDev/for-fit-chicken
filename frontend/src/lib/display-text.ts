// Presentation only: never persist these non-breaking spaces back into CMS data.
export function keepLastWordsTogether(text: string) {
  const words = text.trim().split(/\s+/);
  if (words.length < 2 || words.slice(-2).join(' ').length > 24) return text;
  return `${words.slice(0, -2).join(' ')}${words.length > 2 ? ' ' : ''}${words.slice(-2).join('\u00a0')}`;
}
