import DOMPurify from 'dompurify';
import { marked } from 'marked';

marked.setOptions({
  breaks: true,
});

/**
 * Renders Markdown string to sanitized HTML. Use with v-html only;
 * always sanitized to prevent XSS.
 */
export function renderMarkdown(raw: string): string {
  if (raw.trim() === '') return '';
  const html = marked.parse(raw) as string;
  return DOMPurify.sanitize(html);
}
