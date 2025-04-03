import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule, ActivatedRoute, Router } from '@angular/router';
import { SearchService, SearchParams } from '../../services/search.service';
import { DealCardComponent } from '../deal-card/deal-card.component';
import { Location } from '@angular/common';
import { trigger, state, style, transition, animate, query, stagger } from '@angular/animations';
import { DealResponseDto } from '../../models/deal.model';

@Component({
  selector: 'app-search',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, DealCardComponent],
  templateUrl: './search.component.html',
  styleUrls: ['./search.component.css'],
  animations: [
    trigger('expandCollapse', [
      state('collapsed', style({
        height: '0px',
        overflow: 'hidden',
        opacity: '0'
      })),
      state('expanded', style({
        height: '*',
        overflow: 'hidden',
        opacity: '1'
      })),
      transition('collapsed <=> expanded', [
        animate('300ms ease-in-out')
      ])
    ]),
    trigger('fadeIn', [
      transition(':enter', [
        style({ opacity: 0 }),
        animate('300ms ease-out', style({ opacity: 1 }))
      ])
    ]),
    trigger('staggerAnimation', [
      transition(':enter', [
        style({ transform: 'translateY(20px)', opacity: 0 }),
        animate('500ms ease-out', style({ transform: 'translateY(0)', opacity: 1 }))
      ])
    ])
  ]
})
export class SearchComponent implements OnInit {
  searchParams: SearchParams = {
    page: 1,
    pageSize: 12,
    sortBy: 'relevance'
  };

  searchResponse: any = {
    deals: [],
    totalCount: 0,
    totalPages: 0,
    currentPage: 1,
    pageSize: 12
  };

  isLoading = false;
  errorMessage = '';
  showFilters = true; // Default to showing filters
  searchTimeout: any = null;

  // Filter options
  sortOptions = [
    { value: 'relevance', label: 'Relevance' },
    { value: 'price_asc', label: 'Price: Low to High' },
    { value: 'price_desc', label: 'Price: High to Low' },
    { value: 'rating', label: 'Highest Rated' },
    { value: 'discount', label: 'Highest Discount' },
    { value: 'newest', label: 'Newest First' },
    { value: 'popular', label: 'Most Popular' }
  ];

  packageTypes = [
    'All Inclusive',
    'Adventure',
    'Cultural',
    'Luxury',
    'Budget',
    'Family',
    'Honeymoon',
    'Solo'
  ];

  difficultyLevels = [
    'Easy',
    'Moderate',
    'Challenging',
    'Expert'
  ];

  seasons = [
    'Spring',
    'Summer',
    'Fall',
    'Winter'
  ];

  constructor(
    private searchService: SearchService,
    private route: ActivatedRoute,
    private location: Location,
    private router: Router
  ) {}

  ngOnInit() {
    // Subscribe to query parameters
    this.route.queryParams.subscribe(params => {
      this.applyQueryParams(params);
      this.performSearch();
    });
  }

  applyQueryParams(params: any) {
    // Reset searchParams to defaults first
    this.searchParams = {
      page: 1,
      pageSize: 12,
      sortBy: 'relevance'
    };

    // Apply params from URL
    if (params['searchTerm']) {
      this.searchParams.searchTerm = params['searchTerm'];
    }

    if (params['minPrice']) {
      this.searchParams.minPrice = Number(params['minPrice']);
    }

    if (params['maxPrice']) {
      this.searchParams.maxPrice = Number(params['maxPrice']);
    }

    if (params['minDays']) {
      this.searchParams.minDays = Number(params['minDays']);
    }

    if (params['maxDays']) {
      this.searchParams.maxDays = Number(params['maxDays']);
    }

    if (params['packageType']) {
      this.searchParams.packageType = params['packageType'];
    }

    if (params['sortBy']) {
      this.searchParams.sortBy = params['sortBy'];
    }

    if (params['page']) {
      this.searchParams.page = Number(params['page']);
    }

    if (params['difficultyLevel']) {
      this.searchParams.difficultyLevel = params['difficultyLevel'];
    }

    if (params['seasons']) {
      this.searchParams.seasons = params['seasons'];
    }

    if (params['isInstantBooking'] === 'true') {
      this.searchParams.isInstantBooking = true;
    }

    if (params['isLastMinuteDeal'] === 'true') {
      this.searchParams.isLastMinuteDeal = true;
    }
  }

  performSearch() {
    this.isLoading = true;
    this.errorMessage = '';

    // Update the URL with the current search parameters
    this.updateUrlParams();

    this.searchService.searchDeals(this.searchParams).subscribe({
      next: (response) => {
        this.searchResponse = response;
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Search error:', error);
        this.errorMessage = error.error?.message || 'An error occurred while searching';
        this.isLoading = false;
      }
    });
  }

  updateUrlParams() {
    // Create a params object only with non-default values
    const queryParams: any = {};

    if (this.searchParams.searchTerm) {
      queryParams.searchTerm = this.searchParams.searchTerm;
    }

    if (this.searchParams.minPrice) {
      queryParams.minPrice = this.searchParams.minPrice;
    }

    if (this.searchParams.maxPrice) {
      queryParams.maxPrice = this.searchParams.maxPrice;
    }

    if (this.searchParams.minDays) {
      queryParams.minDays = this.searchParams.minDays;
    }

    if (this.searchParams.maxDays) {
      queryParams.maxDays = this.searchParams.maxDays;
    }

    if (this.searchParams.packageType) {
      queryParams.packageType = this.searchParams.packageType;
    }

    if (this.searchParams.sortBy && this.searchParams.sortBy !== 'relevance') {
      queryParams.sortBy = this.searchParams.sortBy;
    }

    if (this.searchParams.page && this.searchParams.page > 1) {
      queryParams.page = this.searchParams.page;
    }

    if (this.searchParams.difficultyLevel) {
      queryParams.difficultyLevel = this.searchParams.difficultyLevel;
    }

    if (this.searchParams.seasons) {
      queryParams.seasons = this.searchParams.seasons;
    }

    if (this.searchParams.isInstantBooking) {
      queryParams.isInstantBooking = true;
    }

    if (this.searchParams.isLastMinuteDeal) {
      queryParams.isLastMinuteDeal = true;
    }

    // Navigate to update the URL without reloading the page
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: queryParams,
      replaceUrl: true // replace the current URL to avoid creating multiple history entries
    });
  }

  onSearchTermChange() {
    // Debounce the search to avoid too many API calls while typing
    if (this.searchTimeout) {
      clearTimeout(this.searchTimeout);
    }

    this.searchTimeout = setTimeout(() => {
      this.searchParams.page = 1; // Reset to first page
      this.performSearch();
    }, 300);
  }

  onFilterChange() {
    this.searchParams.page = 1; // Reset to first page
    this.performSearch();
  }

  onPageChange(page: number) {
    if (page < 1 || page > this.searchResponse.totalPages) {
      return; // Don't navigate to invalid pages
    }
    this.searchParams.page = page;
    this.performSearch();

    // Scroll to top when changing page
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  onDealClick(dealId: number) {
    // Record the click
    this.searchService.recordDealClick(dealId).subscribe({
      error: (error) => console.error('Error recording click:', error)
    });

    // Navigate to deal detail page
    this.router.navigate(['/deal', dealId]);
  }

  toggleFilters() {
    this.showFilters = !this.showFilters;
  }

  resetFilters() {
    this.searchParams = {
      page: 1,
      pageSize: 12,
      sortBy: 'relevance',
      searchTerm: this.searchParams.searchTerm // Keep the search term
    };
    this.performSearch();
  }

  getPaginationRange(): number[] {
    const totalPages = this.searchResponse.totalPages;
    const currentPage = this.searchResponse.currentPage;

    // Show maximum 5 pages in pagination
    const maxPagesToShow = 5;

    if (totalPages <= maxPagesToShow) {
      // If total pages are less than max, show all pages
      return Array.from({ length: totalPages }, (_, i) => i + 1);
    }

    // Calculate start and end of pagination range
    let start = Math.max(currentPage - Math.floor(maxPagesToShow / 2), 1);
    let end = start + maxPagesToShow - 1;

    // Adjust if end exceeds total pages
    if (end > totalPages) {
      end = totalPages;
      start = Math.max(end - maxPagesToShow + 1, 1);
    }

    return Array.from({ length: end - start + 1 }, (_, i) => start + i);
  }

  getPriceRange() {
    const prices = this.searchResponse.deals.map((deal: DealResponseDto) => deal.price);
    return {
      min: Math.min(...prices),
      max: Math.max(...prices)
    };
  }

  getDaysRange() {
    const days = this.searchResponse.deals.map((deal: DealResponseDto) => deal.daysCount);
    return {
      min: Math.min(...days),
      max: Math.max(...days)
    };
  }

  goBack(): void {
    this.location.back();
  }
}
