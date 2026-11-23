import { useState, type ChangeEvent, type FormEvent } from 'react'
import { submitInquiry } from '../api'
import { clean, handleUrl, siteUrl } from '../links'
import type { InquiryRequest, SiteContent } from '../types'

const empty: InquiryRequest = { name: '', email: '', brand: '', budget: '', message: '', website: '' }
type Errors = Partial<Record<keyof InquiryRequest, string>>

const budgets = ['Under $500', '$500 – $1,500', '$1,500 – $3,000', '$3,000+', 'Not sure yet']

// Same rules as the server's InquiryRequest, so most mistakes are caught before sending.
function validate(f: InquiryRequest): Errors {
  const e: Errors = {}
  if (f.name.trim().length < 2) e.name = 'Please tell us your name.'
  if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(f.email.trim())) e.email = 'Please enter a valid email address.'
  if (f.message.trim().length < 10) e.message = 'Tell us a little more (at least 10 characters).'
  return e
}

export function Contact({ content }: { content: SiteContent }) {
  const { contact } = content
  const [form, setForm] = useState<InquiryRequest>(empty)
  const [errors, setErrors] = useState<Errors>({})
  const [status, setStatus] = useState<'idle' | 'sending' | 'sent' | 'error'>('idle')
  const [serverMessage, setServerMessage] = useState('')

  const update = (key: keyof InquiryRequest) => (e: ChangeEvent<HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement>) => {
    setForm((f) => ({ ...f, [key]: e.target.value }))
    if (errors[key]) setErrors((x) => ({ ...x, [key]: undefined }))
  }

  async function onSubmit(e: FormEvent) {
    e.preventDefault()
    const found = validate(form)
    setErrors(found)
    if (Object.keys(found).length) {
      document.getElementById(`f-${Object.keys(found)[0]}`)?.focus()
      return
    }
    setStatus('sending')
    const result = await submitInquiry(form)
    if (result.ok) {
      setStatus('sent')
      setForm(empty)
    } else {
      setStatus('error')
      setServerMessage(result.message)
      setErrors(result.fieldErrors)
    }
  }

  const field = (key: 'name' | 'email' | 'brand', label: string, type = 'text', autoComplete?: string) => (
    <div className="field">
      <label htmlFor={`f-${key}`}>{label}</label>
      <input
        id={`f-${key}`}
        type={type}
        value={form[key]}
        onChange={update(key)}
        autoComplete={autoComplete}
        aria-invalid={!!errors[key]}
        aria-describedby={errors[key] ? `f-${key}-err` : undefined}
      />
      {errors[key] && (
        <span id={`f-${key}-err`} className="field__error">
          {errors[key]}
        </span>
      )}
    </div>
  )

  return (
    <section id="contact" className="section section--ink" aria-labelledby="contact-title">
      <div className="contact">
        <h2 id="contact-title">
          Let's work <em>together</em>
        </h2>
        <p className="lead">
          Tell us about your brand and what you're launching — we'll come back with ideas within {contact.responseTime}.
        </p>

        <form className="form" onSubmit={onSubmit} noValidate>
          {field('name', 'Your name', 'text', 'name')}
          {field('email', 'Email', 'email', 'email')}
          {field('brand', 'Brand / company', 'text', 'organization')}
          <div className="field">
            <label htmlFor="f-budget">Budget</label>
            <select id="f-budget" value={form.budget} onChange={update('budget')}>
              <option value="">Select one (optional)</option>
              {budgets.map((b) => (
                <option key={b}>{b}</option>
              ))}
            </select>
          </div>
          <div className="field form__full">
            <label htmlFor="f-message">What do you have in mind?</label>
            <textarea
              id="f-message"
              value={form.message}
              onChange={update('message')}
              aria-invalid={!!errors.message}
              aria-describedby={errors.message ? 'f-message-err' : undefined}
            />
            {errors.message && (
              <span id="f-message-err" className="field__error">
                {errors.message}
              </span>
            )}
          </div>

          {/* Honeypot: hidden from people, tempting to bots */}
          <div className="hp" aria-hidden="true">
            <label htmlFor="f-website">Leave this empty</label>
            <input id="f-website" tabIndex={-1} autoComplete="off" value={form.website} onChange={update('website')} />
          </div>

          <div aria-live="polite" className="form__full">
            {status === 'sent' && (
              <p className="form__status form__status--ok">Thank you! Your message is on its way — we'll be in touch soon.</p>
            )}
            {status === 'error' && <p className="form__status form__status--err">{serverMessage}</p>}
          </div>

          <button className="btn btn--light form__submit" type="submit" disabled={status === 'sending'}>
            {status === 'sending' ? 'Sending…' : 'Send message'}
          </button>
        </form>

        <div className="contact__links">
          <a href={`mailto:${clean(contact.email)}`}>{contact.email}</a>
          <a href={siteUrl(contact.website)}>{contact.website}</a>
          <a href={handleUrl('instagram', contact.instagram)} target="_blank" rel="noreferrer">
            Instagram {contact.instagram}
          </a>
          <a href={handleUrl('tiktok', contact.tikTok)} target="_blank" rel="noreferrer">
            TikTok {contact.tikTok}
          </a>
        </div>
      </div>
    </section>
  )
}

const YEAR = new Date().getFullYear()

export function Footer({ brand }: { brand: string }) {
  return (
    <footer className="footer">
      <div className="footer__inner">
        <span>
          © {YEAR} {brand}
        </span>
        <a href="#top">Back to top ↑</a>
      </div>
    </footer>
  )
}
