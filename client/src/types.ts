// Mirrors server/AwwTogether.Api/Models/SiteContent.cs

export interface SiteContent {
  brandName: string
  tagline: string
  introVideo: { url: string; posterUrl: string | null; title: string } | null
  creators: Creator[]
  aboutHeadline: string | null
  about: string
  offerings: Offering[]
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
  bio: string | null
}

export interface Offering {
  title: string
  description: string
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
  role: string | null
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
