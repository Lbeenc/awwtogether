import { useEffect, useState } from 'react'
import { getContent } from './api'
import { Contact, Footer } from './components/Contact'
import { Hero, Marquee } from './components/Hero'
import { Nav } from './components/Nav'
import { About, PhotoPortfolio, Services, Testimonials, VideoPortfolio, WhatIsUgc, WhyUgc } from './components/Sections'
import type { SiteContent } from './types'

export default function App() {
  const [content, setContent] = useState<SiteContent | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    const ctrl = new AbortController()
    getContent(ctrl.signal)
      .then(setContent)
      .catch((e: unknown) => {
        if (!ctrl.signal.aborted) setError(e instanceof Error ? e.message : 'Something went wrong')
      })
    return () => ctrl.abort()
  }, [])

  if (error || !content) {
    return (
      <div className="status-screen" role={error ? 'alert' : 'status'}>
        <p className="status-screen__logo">
          Aww<em>Together</em>
        </p>
        <p>{error ?? 'Loading…'}</p>
        {error && (
          <button className="btn btn--dark" onClick={() => location.reload()}>
            Try again
          </button>
        )}
      </div>
    )
  }

  return (
    <>
      <a href="#main" className="skip-link">
        Skip to content
      </a>
      <Nav />
      <main id="main">
        <Hero content={content} />
        <Marquee />
        <About content={content} />
        <WhatIsUgc content={content} />
        <WhyUgc content={content} />
        <PhotoPortfolio content={content} />
        <VideoPortfolio content={content} />
        <Testimonials content={content} />
        <Services content={content} />
        <Contact content={content} />
      </main>
      <Footer brand={content.brandName} />
    </>
  )
}
