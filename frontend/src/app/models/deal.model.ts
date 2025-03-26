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
  startPoint: string;
  endPoint: string;
  duration: string;
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
