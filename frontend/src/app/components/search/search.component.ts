import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule, ActivatedRoute, Router } from '@angular/router';
import { SearchService, SearchParams } from '../../services/search.service';
import { DealCardComponent } from '../deal-card/deal-card.component';
import { Location } from '@angular/common';

interface Deal {
  price: number;
  daysCount: number;
}

@Component({
  selector: 'app-search',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, DealCardComponent],
  templateUrl: './search.component.html',
  styleUrls: ['./search.component.css']
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
      if (params['searchTerm']) {
        this.searchParams.searchTerm = params['searchTerm'];
      }
      this.performSearch();
    });
  }

  performSearch() {
    this.isLoading = true;
    this.errorMessage = '';

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

  onSearchTermChange() {
    this.searchParams.page = 1; // Reset to first page
    this.performSearch();
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
    const prices = this.searchResponse.deals.map((deal: Deal) => deal.price);
    return {
      min: Math.min(...prices),
      max: Math.max(...prices)
    };
  }

  getDaysRange() {
    const days = this.searchResponse.deals.map((deal: Deal) => deal.daysCount);
    return {
      min: Math.min(...days),
      max: Math.max(...days)
    };
  }

  goBack(): void {
    this.location.back();
  }
}
