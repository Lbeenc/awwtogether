import type { SiteContent } from '../types'
import { Sparkle } from './Icons'
import { Media, VideoPlayer } from './Media'

type Props = { content: SiteContent }

export function About({ content }: Props) {
  return (
    <section id="about" className="about" aria-labelledby="about-title">
      <div className="about__art">
        <figure className="about__intro">
          <div className="about__photo">
            {content.introVideo ? (
              <VideoPlayer src={content.introVideo.url} poster={content.introVideo.posterUrl} title={content.introVideo.title} />
            ) : (
              <Media src={null} alt="" label="Photo of you two together" />
            )}
          </div>
          {content.introVideo && <figcaption className="about__caption">{content.introVideo.title}</figcaption>}
        </figure>
        <p className="about__seal" aria-hidden="true">
          AWW
          <br />✦<br />
          TOGETHER
        </p>
      </div>
      <div className="about__copy">
        <h2 id="about-title" className="heading">
          About <em>us</em>
        </h2>
        <p className="lead">{content.about}</p>
        <div className="creators">
          {content.creators.map((c, i) => (
            <div className="creator" key={i}>
              <h3>{c.name}</h3>
              <p>{c.specialty}</p>
            </div>
          ))}
        </div>
      </div>
    </section>
  )
}

export function WhatIsUgc({ content }: Props) {
  return (
    <section className="section section--blush" aria-labelledby="ugc-title">
      <div className="container split">
        <div>
          <h2 id="ugc-title" className="heading">
            What is <em>UGC?</em>
          </h2>
          <p className="lead">{content.whatIsUgc}</p>
          <div className="sparkles">
            <Sparkle />
            <Sparkle />
            <Sparkle />
          </div>
        </div>
        <div className="split__pair">
          <div>
            <Media src={content.photos[0]?.mediaUrl ?? null} alt="" label="Product shot" />
          </div>
          <div>
            <Media src={content.photos[1]?.mediaUrl ?? null} alt="" label="Lifestyle shot" />
          </div>
        </div>
      </div>
    </section>
  )
}

export function WhyUgc({ content }: Props) {
  return (
    <section className="section section--paper" aria-labelledby="why-title">
      <div className="container">
        <h2 id="why-title" className="heading center">
          Why <em>UGC?</em>
        </h2>
        <div className="stats">
          {content.stats.map((s, i) => (
            <div className="stat" key={i}>
              <p className="stat__value">{s.value}</p>
              <p>{s.label}</p>
            </div>
          ))}
        </div>
      </div>
    </section>
  )
}

export function PhotoPortfolio({ content }: Props) {
  return (
    <section id="portfolio" className="section section--blush" aria-labelledby="photos-title">
      <div className="container">
        <div className="section-head">
          <h2 id="photos-title" className="heading">
            Photo <em>portfolio</em>
          </h2>
          <p className="eyebrow">Selected brand work</p>
        </div>
        <div className="grid-4">
          {content.photos.map((p, i) => (
            <article className="card" key={i}>
              <div className="card__media">
                <Media src={p.mediaUrl} alt={`${p.title} for ${p.brand}`} label="Image" />
              </div>
              <h3>
                {p.title}, {p.brand}
              </h3>
              <p>{p.description}</p>
              {p.link && (
                <a href={p.link} target="_blank" rel="noreferrer">
                  View project →
                </a>
              )}
            </article>
          ))}
        </div>
      </div>
    </section>
  )
}

export function VideoPortfolio({ content }: Props) {
  return (
    <section className="section section--paper" aria-labelledby="videos-title">
      <div className="container">
        <div className="section-head">
          <h2 id="videos-title" className="heading">
            Video <em>portfolio</em>
          </h2>
          <p className="eyebrow">TikTok · Reels · Shorts</p>
        </div>
        <div className="grid-4">
          {content.videos.map((v, i) => (
            <article className="phone-card" key={i}>
              <div className="phone">
                <Media src={v.mediaUrl} poster={v.posterUrl} alt={`${v.brand} UGC video`} label={v.title} kind="video" />
              </div>
              <h3>{v.brand}</h3>
              <p>{v.description}</p>
              {v.link && (
                <a href={v.link} target="_blank" rel="noreferrer">
                  Watch →
                </a>
              )}
            </article>
          ))}
        </div>
      </div>
    </section>
  )
}

export function Testimonials({ content }: Props) {
  return (
    <section className="section section--cream" aria-labelledby="love-title">
      <div className="container love">
        <h2 id="love-title" className="love__title">
          Client
          <br />
          <em>love</em>
        </h2>
        <div className="love__grid">
          {content.testimonials.map((t, i) => (
            <figure className="quote" key={i}>
              <p className="quote__mark" aria-hidden="true">
                “
              </p>
              <blockquote>{t.quote}</blockquote>
              <figcaption>
                <span className="quote__avatar" aria-hidden="true" />
                <span>
                  <strong>{t.name}</strong>
                  {t.role}
                </span>
              </figcaption>
            </figure>
          ))}
        </div>
      </div>
    </section>
  )
}

export function Services({ content }: Props) {
  return (
    <section id="services" className="section section--paper" aria-labelledby="services-title">
      <div className="container">
        <h2 id="services-title" className="heading center">
          Services &amp; <em>rates</em>
        </h2>
        <div className="services">
          {content.services.map((s, i) => (
            <div className="service" key={i}>
              <h3>{s.title}</h3>
              <p>{s.description}</p>
              <ul className="tiers">
                {s.tiers.map((t, j) => (
                  <li key={j}>
                    <span>{t.label}</span>
                    <strong>{t.price}</strong>
                  </li>
                ))}
              </ul>
            </div>
          ))}
        </div>
        <p className="center">
          Need usage rights, raw footage or a custom bundle? <a href="#contact">Let's chat.</a>
        </p>
      </div>
    </section>
  )
}
