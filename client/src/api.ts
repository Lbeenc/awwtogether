import type { InquiryRequest, SiteContent } from './types'

export async function getContent(signal?: AbortSignal): Promise<SiteContent> {
  const res = await fetch('/api/content', { signal })
  if (!res.ok) throw new Error(`Couldn't load the site (${res.status})`)
  return res.json() as Promise<SiteContent>
}

export type SubmitResult =
  | { ok: true }
  | { ok: false; message: string; fieldErrors: Partial<Record<keyof InquiryRequest, string>> }

/** Posts the contact form. Maps ASP.NET validation ProblemDetails back to form fields. */
export async function submitInquiry(body: InquiryRequest): Promise<SubmitResult> {
  let res: Response
  try {
    res = await fetch('/api/inquiries', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body),
    })
  } catch {
    return { ok: false, message: 'Network error — please check your connection and try again.', fieldErrors: {} }
  }

  if (res.ok) return { ok: true }

  if (res.status === 429) {
    return { ok: false, message: "You've sent a few messages already — please try again in a little while.", fieldErrors: {} }
  }

  const fieldErrors: Partial<Record<keyof InquiryRequest, string>> = {}
  try {
    const problem = (await res.json()) as { errors?: Record<string, string[]> }
    for (const [key, messages] of Object.entries(problem.errors ?? {})) {
      const field = (key.charAt(0).toLowerCase() + key.slice(1)) as keyof InquiryRequest
      fieldErrors[field] = messages[0]
    }
  } catch {
    /* not JSON */
  }
  return { ok: false, message: 'Something went wrong sending your message. Please try again.', fieldErrors }
}
