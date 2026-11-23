import { ImageIcon, PlayIcon } from './Icons'

interface MediaProps {
  src: string | null
  alt: string
  /** Shown in the empty frame until real media is added. */
  label: string
  kind?: 'image' | 'video'
}

const isVideo = (src: string) => /\.(mp4|webm|mov)$/i.test(src)

/** Shows a photo or video if one is set in content.json, otherwise a labelled placeholder. */
export function Media({ src, alt, label, kind = 'image' }: MediaProps) {
  if (src) {
    return isVideo(src) ? (
      <video className="media" src={src} muted loop playsInline autoPlay preload="metadata" aria-label={alt} />
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
