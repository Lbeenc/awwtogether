import { useRef, useState } from 'react'
import { ImageIcon, PlayIcon } from './Icons'

interface MediaProps {
  src: string | null
  alt: string
  /** Shown in the empty frame until real media is added. */
  label: string
  kind?: 'image' | 'video'
  poster?: string | null
}

const isVideo = (src: string) => /\.(mp4|webm|mov)$/i.test(src)

/** Shows a photo or video if one is set in content.json, otherwise a labelled placeholder. */
export function Media({ src, alt, label, kind = 'image', poster }: MediaProps) {
  if (src) {
    return isVideo(src) ? (
      <VideoPlayer src={src} poster={poster ?? null} title={alt} />
    ) : (
      <img className="media" src={src} alt={alt} loading="lazy" />
    )
  }
  return (
    <div className="placeholder" role="img" aria-label={`${label} (placeholder)`}>
      {kind === 'video' ? <PlayIcon /> : <ImageIcon />}
      <span aria-hidden="true">{label}</span>
    </div>
  )
}

/**
 * Poster image with a play button. Nothing downloads until the visitor presses play,
 * then it plays with sound and native controls. Starting one video pauses any other.
 */
export function VideoPlayer({ src, poster, title }: { src: string; poster: string | null; title: string }) {
  const ref = useRef<HTMLVideoElement>(null)
  const [started, setStarted] = useState(false)

  const play = () => {
    document.querySelectorAll('video').forEach((v) => v !== ref.current && v.pause())
    setStarted(true)
    void ref.current?.play()
  }

  return (
    <div className="video">
      <video
        ref={ref}
        className="media"
        src={src}
        poster={poster ?? undefined}
        preload="none"
        playsInline
        controls={started}
        aria-label={title}
        onPlay={(e) => document.querySelectorAll('video').forEach((v) => v !== e.currentTarget && v.pause())}
      />
      {!started && (
        <button type="button" className="video__play" onClick={play} aria-label={`Play video: ${title}`}>
          <svg width="26" height="26" viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
            <path d="M8 5.5v13a1 1 0 0 0 1.5.86l10.5-6.5a1 1 0 0 0 0-1.72L9.5 4.64A1 1 0 0 0 8 5.5Z" />
          </svg>
        </button>
      )}
    </div>
  )
}
