import DOMPurify from 'dompurify'
import { marked } from 'marked'

marked.setOptions({
  breaks: true
})

function autoLinkPlainUrls(raw: string): string {
  const urlRegex = /(^|\s)(https?:\/\/[^\s)]+)(?=$|\s)/g
  return raw.replace(urlRegex, (_, prefix: string, url: string) => {
    return `${prefix}<${url}>`
  })
}

/**
 * Renders Markdown string to sanitized HTML. Use with v-html only;
 * always sanitized to prevent XSS.
 */
export function renderMarkdown(raw: string): string {
  if (raw.trim() === '') return ''

  const prepared = autoLinkPlainUrls(raw)
  const html = marked.parse(prepared) as string
  const sanitized = DOMPurify.sanitize(html)
  // Open all links in new tab (journal and any other markdown content)
  return sanitized.replace(/<a\s/gi, '<a target="_blank" rel="noopener noreferrer" ')
}
