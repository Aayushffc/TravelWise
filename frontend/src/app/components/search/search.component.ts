import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule, ActivatedRoute } from '@angular/router';
import { SearchService, SearchParams } from '../../services/search.service';

interface Deal {
  price: number;
  daysCount: number;
}

@Component({
  selector: 'app-search',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
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
    private route: ActivatedRoute
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
    this.searchParams.page = page;
    this.performSearch();
  }

  onDealClick(dealId: number) {
    this.searchService.recordDealClick(dealId).subscribe({
      error: (error) => console.error('Error recording click:', error)
    });
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
}
