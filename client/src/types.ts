// Mirrors server/AwwTogether.Api/Models/SiteContent.cs

export interface SiteContent {
  brandName: string
  tagline: string
  introVideo: { url: string; posterUrl: string | null; title: string } | null
  creators: Creator[]
  about: string
  whatIsUgc: string
  stats: Stat[]
  photos: PortfolioItem[]
  videos: PortfolioItem[]
  testimonials: Testimonial[]
  services: Service[]
  contact: ContactInfo
}

export interface Creator {
  name: string
  specialty: string
  photoUrl: string | null
}

export interface Stat {
  value: string
  label: string
}

export interface PortfolioItem {
  title: string
  brand: string
  description: string
  mediaUrl: string | null
  link: string | null
  posterUrl?: string | null
}

export interface Testimonial {
  quote: string
  name: string
  role: string
}

export interface Service {
  title: string
  description: string
  tiers: { label: string; price: string }[]
}

export interface ContactInfo {
  email: string
  website: string
  instagram: string
  tikTok: string
  responseTime: string
}

export interface InquiryRequest {
  name: string
  email: string
  brand: string
  budget: string
  message: string
  /** Honeypot — always empty for real visitors. */
  website: string
}
