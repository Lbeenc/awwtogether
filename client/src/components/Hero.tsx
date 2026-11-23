import type { SiteContent } from '../types'
import { InstagramIcon, LinkIcon, MailIcon, Sunburst, TikTokIcon } from './Icons'
import { Media } from './Media'
import { clean, handleUrl, siteUrl } from '../links'

export function Hero({ content }: { content: SiteContent }) {
  const { contact, creators } = content
  const [a, b] = creators

  return (
    <section id="top" className="hero">
      <Sunburst className="hero__burst" />
      <div className="hero__inner">
        <div className="hero__copy">
          <p className="eyebrow">Hi, we're</p>
          <h1 className="hero__title">
            Aww
            <br />
            <em>Together</em>
          </h1>
          <p className="hero__tagline">{content.tagline}</p>
          <div className="hero__actions">
            <a href="#contact" className="btn btn--dark">
              Work with us
            </a>
            <a className="icon-btn" href={siteUrl(contact.website)} aria-label="Website">
              <LinkIcon />
            </a>
            <a className="icon-btn" href={`mailto:${clean(contact.email)}`} aria-label="Email">
              <MailIcon />
            </a>
            <a className="icon-btn" href={handleUrl('instagram', contact.instagram)} aria-label="Instagram" target="_blank" rel="noreferrer">
              <InstagramIcon />
            </a>
            <a className="icon-btn" href={handleUrl('tiktok', contact.tikTok)} aria-label="TikTok" target="_blank" rel="noreferrer">
              <TikTokIcon />
            </a>
          </div>
        </div>

        <div className="hero__art">
          <div className="arch arch--left">
            <Media src={a?.photoUrl ?? null} alt={a?.name ?? ''} label={`Portrait — ${a?.name ?? 'Creator 1'}`} />
          </div>
          <div className="arch arch--right">
            <Media src={b?.photoUrl ?? null} alt={b?.name ?? ''} label={`Portrait — ${b?.name ?? 'Creator 2'}`} />
          </div>
          <p className="badge" aria-hidden="true">
            two
            <br />
            is better
            <br />
            than one
          </p>
        </div>
      </div>
    </section>
  )
}

export function Marquee() {
  const run = (
    <span>
      {Array.from({ length: 6 }, (_, i) => (
        <span key={i}>
          <span>UGC CREATOR DUO</span>
          <span>✦</span>
          <span>AWWTOGETHER</span>
          <span>✦</span>
        </span>
      ))}
    </span>
  )
  return (
    <div className="marquee" aria-hidden="true">
      {/* Two identical runs so the -50% loop is seamless */}
      <div className="marquee__track">
        {run}
        {run}
      </div>
    </div>
  )
}
