export interface Deal {
  id: number;
  title: string;
  locationId: number;
  location?: {
    id: number;
    name: string;
  };
  price: number;
  discountedPrice?: number;
  discountPercentage?: number;
  rating?: number;
  daysCount: number;
  nightsCount: number;
  description: string;
  photos?: string[];
  elderlyFriendly: boolean;
  internetIncluded: boolean;
  travelIncluded: boolean;
  mealsIncluded: boolean;
  sightseeingIncluded: boolean;
  stayIncluded: boolean;
  airTransfer: boolean;
  roadTransfer: boolean;
  trainTransfer: boolean;
  travelCostIncluded: boolean;
  guideIncluded: boolean;
  photographyIncluded: boolean;
  insuranceIncluded: boolean;
  visaIncluded: boolean;
  itinerary: ItineraryDay[];
  packageOptions: PackageOption[];
  mapUrl?: string;
  policies: Policy[];
  packageType: string;
  isActive: boolean;
  createdAt: Date;
  updatedAt: Date;
  userId: string;
  clickCount?: number;
  tags?: string[];
  headlines?: string[];
}

export interface ItineraryDay {
  dayNumber: number;
  title: string;
  description: string;
  activities: string[];
}

export interface PackageOption {
  name: string;
  description: string;
  price: number;
  inclusions: string[];
}

export interface Policy {
  title: string;
  description: string;
}

export interface DealResponseDto extends Deal {
  status: string;
}

export interface DealCreateDto {
  title: string;
  locationId: number;
  price: number;
  discountedPrice?: number;
  discountPercentage?: number;
  rating?: number;
  daysCount: number;
  nightsCount: number;
  description: string;
  photos: string[];
  packageType: string;
  isActive: boolean;
  status: string;
  createdAt: Date;
  updatedAt: Date;
  userId: string;
  headlines?: string[];
  tags?: string[];
  seasons?: string[];
  difficultyLevel?: string;
  maxGroupSize?: number;
  minGroupSize?: number;
  isInstantBooking?: boolean;
  isLastMinuteDeal?: boolean;
  validFrom?: Date;
  validUntil?: Date;
  cancellationPolicy?: string;
  refundPolicy?: string;
  languages?: string[];
  requirements?: string[];
  restrictions?: string[];
  itinerary: ItineraryDay[];
  packageOptions?: PackageOption[];
  policies?: Policy[];
}

export interface DealUpdateDto {
  title?: string;
  locationId?: number;
  price?: number;
  discountedPrice?: number;
  discountPercentage?: number;
  rating?: number;
  daysCount?: number;
  nightsCount?: number;
  description?: string;
  photos?: string[];
  packageType?: string;
  isActive?: boolean;
  status?: string;
  updatedAt: Date;
  headlines?: string[];
  tags?: string[];
  seasons?: string[];
  difficultyLevel?: string;
  maxGroupSize?: number;
  minGroupSize?: number;
  isInstantBooking?: boolean;
  isLastMinuteDeal?: boolean;
  validFrom?: Date;
  validUntil?: Date;
  cancellationPolicy?: string;
  refundPolicy?: string;
  languages?: string[];
  requirements?: string[];
  restrictions?: string[];
  itinerary?: ItineraryDay[];
  packageOptions?: PackageOption[];
  policies?: Policy[];
}

export interface DealToggleStatusDto {
  isActive: boolean;
}
