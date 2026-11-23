import type { SVGProps } from 'react'

const base: SVGProps<SVGSVGElement> = {
  width: 20,
  height: 20,
  viewBox: '0 0 24 24',
  fill: 'none',
  stroke: 'currentColor',
  strokeWidth: 1.8,
  strokeLinecap: 'round',
  strokeLinejoin: 'round',
  'aria-hidden': true,
}

export const LinkIcon = () => (
  <svg {...base}>
    <path d="M10 13a5 5 0 0 0 7.5.5l3-3a5 5 0 0 0-7-7l-1.7 1.7" />
    <path d="M14 11a5 5 0 0 0-7.5-.5l-3 3a5 5 0 0 0 7 7l1.7-1.7" />
  </svg>
)

export const MailIcon = () => (
  <svg {...base}>
    <rect x="3" y="5" width="18" height="14" rx="2" />
    <path d="m3 7 9 6 9-6" />
  </svg>
)

export const InstagramIcon = () => (
  <svg {...base}>
    <rect x="3" y="3" width="18" height="18" rx="5" />
    <circle cx="12" cy="12" r="4" />
    <circle cx="17.5" cy="6.5" r=".6" />
  </svg>
)

export const TikTokIcon = () => (
  <svg {...base}>
    <path d="M14 3v11.5a3.5 3.5 0 1 1-3.5-3.5" />
    <path d="M14 3c.5 2.8 2.4 4.6 5 4.8" />
  </svg>
)

export const ImageIcon = () => (
  <svg {...base} width={28} height={28} strokeWidth={1.5}>
    <rect x="3" y="4" width="18" height="16" rx="2" />
    <circle cx="9" cy="10" r="2" />
    <path d="m3 18 5-5 4 4 3-3 6 6" />
  </svg>
)

export const PlayIcon = () => (
  <svg {...base} width={28} height={28} strokeWidth={1.5}>
    <circle cx="12" cy="12" r="9" />
    <path d="m10 8.5 5 3.5-5 3.5Z" />
  </svg>
)

export const Sparkle = () => (
  <svg width="22" height="22" viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
    <path d="M12 0c.8 6.2 5 10.8 12 12-7 1.2-11.2 5.8-12 12-.8-6.2-5-10.8-12-12C7 10.8 11.2 6.2 12 0Z" />
  </svg>
)

export const Sunburst = ({ className }: { className?: string }) => (
  <svg className={className} viewBox="0 0 200 200" fill="none" stroke="#9A5A5F" strokeWidth="1.2" aria-hidden="true">
    {Array.from({ length: 12 }, (_, i) => {
      const a = (i * Math.PI) / 6
      return <line key={i} x1="100" y1="100" x2={100 + 100 * Math.sin(a)} y2={100 - 100 * Math.cos(a)} />
    })}
  </svg>
)
