const fs = require('fs');
const path = require('path');

const uiDir = 'F:/Program Files/My pro/project/Developments/Service Marketplace system/Service_Marketplace_UI/Service_Marketplace_UI';
const stagingDir = 'f:/Program Files/My pro/project/Developments/Service Marketplace system/Service_Marketplace_API/Service_Marketplace_API/Service_Marketplace_API/ui_staging';

function writeFileBoth(relPath, content) {
  const p1 = path.join(uiDir, relPath);
  const p2 = path.join(stagingDir, relPath);
  fs.mkdirSync(path.dirname(p1), { recursive: true });
  fs.mkdirSync(path.dirname(p2), { recursive: true });
  fs.writeFileSync(p1, content, 'utf8');
  fs.writeFileSync(p2, content, 'utf8');
  console.log('Updated:', relPath);
}

// 1. Update features/dashboard/dashboard.ts
const dashboardTs = `import { Component, inject, OnInit, signal, computed, PLATFORM_ID } from '@angular/core';
import { CommonModule, isPlatformBrowser } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AiService } from '../../core/services/ai.service';
import { MarketplaceService } from '../../core/services/marketplace.service';
import { AuthService } from '../../core/services/auth.service';
import { ServiceDiscoveryResult } from '../../core/models/ai.models';
import { ProviderServiceListing, ReceiverJob, Category } from '../../core/models/marketplace.models';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.css'
})
export class DashboardComponent implements OnInit {
  private readonly aiService = inject(AiService);
  private readonly marketplaceService = inject(MarketplaceService);
  private readonly platformId = inject(PLATFORM_ID);
  readonly auth = inject(AuthService);

  // AI Search state
  aiQuery = signal<string>('I need someone to wash my car at my home in Negombo this weekend');
  aiLocation = signal<string>('Negombo');
  isAiSearching = signal<boolean>(false);
  aiResult = signal<ServiceDiscoveryResult | null>(null);
  aiError = signal<string | null>(null);

  // Marketplace Listings state
  activeTab = signal<'all' | 'services' | 'jobs'>('all');
  services = signal<ProviderServiceListing[]>([]);
  jobs = signal<ReceiverJob[]>([]);
  categories = signal<Category[]>([]);
  isLoadingMarketplace = signal<boolean>(false);
  selectedCategory = signal<number | null>(null);
  locationFilter = signal<string>('');

  // Sample prompt chips
  readonly samplePrompts = [
    'Wash my car at home in Negombo this weekend',
    'Repair laptop blue screen crash near Negombo',
    'Deep clean my 3-bedroom house under Rs. 10000',
    'AC gas charging and servicing in Katunayake'
  ];

  // Computed filtered lists based on selected category chip
  readonly filteredServices = computed(() => {
    const cat = this.selectedCategory();
    const list = this.services();
    return cat ? list.filter(s => s.categoryId === cat) : list;
  });

  readonly filteredJobs = computed(() => {
    const cat = this.selectedCategory();
    const list = this.jobs();
    if (!cat) return list; const catObj = this.categories().find(c => c.id === cat); return catObj ? list.filter(j => j.categoryName?.toLowerCase() === catObj.name.toLowerCase()) : list;
  });

  readonly totalCreatedCount = computed(() => {
    return this.filteredServices().length + this.filteredJobs().length;
  });

  ngOnInit(): void {
    if (isPlatformBrowser(this.platformId)) {
      this.loadMarketplaceData();
      this.loadCategories();
    }
  }

  runAiSearch(customQuery?: string): void {
    const query = customQuery || this.aiQuery().trim();
    if (!query) return;

    this.aiQuery.set(query);
    this.isAiSearching.set(true);
    this.aiError.set(null);

    this.aiService.discoverServices({
      query,
      preferredLocation: this.aiLocation().trim() || undefined
    }).subscribe({
      next: (res) => {
        this.isAiSearching.set(false);
        if (res.success && res.data) {
          this.aiResult.set(res.data);
        } else {
          this.aiError.set(res.message || 'No matches found.');
        }
      },
      error: (err) => {
        this.isAiSearching.set(false);
        this.aiError.set(err.error?.message || 'AI service discovery encountered an error.');
      }
    });
  }

  loadMarketplaceData(): void {
    this.isLoadingMarketplace.set(true);

    if (this.auth.isMarketplaceUser()) {
      // Role ID = 1: Load ONLY services and jobs created by the authenticated user
      this.marketplaceService.getMyServices().subscribe({
        next: (res) => {
          this.services.set(res.success && res.data ? res.data : []);
          this.isLoadingMarketplace.set(false);
        },
        error: () => {
          this.services.set([]);
          this.isLoadingMarketplace.set(false);
        }
      });

      this.marketplaceService.getMyJobs().subscribe({
        next: (res) => {
          this.jobs.set(res.success && res.data ? res.data : []);
        },
        error: () => {
          this.jobs.set([]);
        }
      });
    } else {
      // Guests / Admins: Load all public services and jobs with optional server filters
      const filter = {
        location: this.locationFilter().trim() || undefined,
        categoryId: this.selectedCategory() || undefined
      };

      this.marketplaceService.getServices(filter).subscribe({
        next: (res) => {
          this.services.set(res.success && res.data ? res.data : []);
          this.isLoadingMarketplace.set(false);
        },
        error: () => {
          this.services.set([]);
          this.isLoadingMarketplace.set(false);
        }
      });

      this.marketplaceService.getJobs(filter).subscribe({
        next: (res) => {
          this.jobs.set(res.success && res.data ? res.data : []);
        },
        error: () => {
          this.jobs.set([]);
        }
      });
    }
  }

  loadCategories(): void {
    this.marketplaceService.getCategories().subscribe({
      next: (res) => {
        if (res.success && res.data) {
          this.categories.set(res.data);
        }
      }
    });
  }

  setTab(tab: 'all' | 'services' | 'jobs'): void {
    this.activeTab.set(tab);
  }

  selectCategory(catId: number | null): void {
    this.selectedCategory.set(catId);
    if (!this.auth.isMarketplaceUser()) {
      this.loadMarketplaceData();
    }
  }

  onLocationFilterChange(): void {
    if (!this.auth.isMarketplaceUser()) {
      this.loadMarketplaceData();
    }
  }
}
`;
writeFileBoth('src/app/features/dashboard/dashboard.ts', dashboardTs);

// 2. Update features/dashboard/dashboard.html
const dashboardHtml = `<div class="dashboard-wrapper">
  <!-- Hero Section: AI Service Discovery -->
  <section class="hero-section">
    <div class="hero-container">
      <div class="hero-badge">
        <span class="sparkle-icon">✨</span>
        <span>Google Gemini AI Powered Service Discovery</span>
      </div>

      <h1 class="hero-title">
        Describe Any Service You Need. <br/>
        <span class="gradient-text">Our AI Matches Verified Providers In Seconds.</span>
      </h1>
      
      <p class="hero-subtitle">
        Single account marketplace connecting Sri Lankan service providers and customers with natural-language matching.
      </p>

      <!-- AI Search Box Card -->
      <div class="ai-search-card card-glass pulse-glow">
        <form (ngSubmit)="runAiSearch()" class="ai-search-form">
          <div class="ai-input-group main-query">
            <svg class="search-icon" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <circle cx="11" cy="11" r="8"/><path d="m21 21-4.3-4.3"/>
            </svg>
            <input 
              type="text" 
              class="ai-input" 
              placeholder="e.g. I need someone to wash my car at home in Negombo this weekend" 
              [ngModel]="aiQuery()" 
              (ngModelChange)="aiQuery.set($event)" 
              name="aiQuery" 
              required
            />
          </div>

          <div class="ai-input-group location-group">
            <svg class="search-icon" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M20 10c0 6-8 12-8 12s-8-6-8-12a8 8 0 0 1 16 0Z"/><circle cx="12" cy="10" r="3"/>
            </svg>
            <input 
              type="text" 
              class="ai-input" 
              placeholder="City / Location" 
              [ngModel]="aiLocation()" 
              (ngModelChange)="aiLocation.set($event)" 
              name="aiLocation" 
            />
          </div>

          <button type="submit" class="btn btn-primary ai-submit-btn" [disabled]="isAiSearching()">
            @if (isAiSearching()) {
              <span class="spinner"></span>
              <span>Matching...</span>
            } @else {
              <span>AI Match</span>
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2"><path d="m5 12 7-7 7 7"/><path d="M12 19V5"/></svg>
            }
          </button>
        </form>

        <!-- Quick Prompt Chips -->
        <div class="prompt-chips">
          <span class="chips-label">Try asking:</span>
          @for (prompt of samplePrompts; track prompt) {
            <button type="button" class="chip-btn" (click)="runAiSearch(prompt)">
              {{ prompt }}
            </button>
          }
        </div>
      </div>

      <!-- AI Search Results Panel -->
      @if (aiError()) {
        <div class="alert-error mt-4">
          <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="12" cy="12" r="10"/><line x1="12" y1="8" x2="12" y2="12"/><line x1="12" y1="16" x2="12.01" y2="16"/></svg>
          <span>{{ aiError() }}</span>
        </div>
      }

      @if (aiResult(); as result) {
        <div class="ai-result-panel card-glass">
          <div class="ai-result-header">
            <div class="intent-badge-group">
              <span class="badge badge-primary">Intent: {{ result.extractedRequirements.detectedIntent }}</span>
              @if (result.extractedRequirements.categoryName) {
                <span class="badge badge-secondary">{{ result.extractedRequirements.categoryName }}</span>
              }
              @if (result.extractedRequirements.variantName) {
                <span class="badge badge-success">{{ result.extractedRequirements.variantName }}</span>
              }
            </div>

            <div class="ai-meta-pills">
              <span class="meta-pill">Confidence: {{ (result.extractedRequirements.confidenceScore * 100).toFixed(0) }}%</span>
              <span class="meta-pill">Engine: {{ result.engineUsed }}</span>
            </div>
          </div>

          <!-- Matched Providers List -->
          <div class="matched-providers-section">
            <h3 class="matched-title">✨ AI Matched Service Providers</h3>

            @if (result.matchedProviders.length === 0) {
              <div class="empty-state">
                <p>No verified providers currently found for this specific query.</p>
                @if (auth.isMarketplaceUser()) {
                  <a routerLink="/receiver" class="btn btn-outline btn-sm mt-3">Post as Job Request</a>
                }
              </div>
            } @else {
              <div class="provider-cards-grid">
                @for (provider of result.matchedProviders; track provider.providerServiceId) {
                  <div class="provider-card card-glass card-hoverable">
                    <div class="provider-card-header">
                      <div>
                        <h4 class="provider-name">{{ provider.businessOrProviderName }}</h4>
                        <div class="provider-service-title">{{ provider.serviceVariantName }}</div>
                      </div>
                      <div class="score-pill">
                        <span class="score-val">{{ (provider.matchScore * 100).toFixed(0) }}%</span>
                        <span class="score-label">Match</span>
                      </div>
                    </div>

                    <div class="provider-badges">
                      @if (provider.isVerified) {
                        <span class="badge badge-verified">✓ Verified Provider</span>
                      }
                      <span class="rating-badge">★ {{ provider.ratingAverage.toFixed(1) }}</span>
                      <span class="jobs-done-badge">{{ provider.completedJobsCount }} jobs done</span>
                    </div>

                    <p class="match-reason">{{ provider.matchReason }}</p>

                    <div class="provider-card-footer">
                      <div class="price-box">
                        <span class="price-val">Rs. {{ provider.startingPrice.toLocaleString() }}</span>
                        <span class="price-unit">/ {{ provider.priceUnit }}</span>
                      </div>
                      <button type="button" class="btn btn-primary btn-sm" (click)="setTab('services')">View Details</button>
                    </div>
                  </div>
                }
              </div>
            }
          </div>
        </div>
      }
    </div>
  </section>

  <!-- Marketplace Ongoing Activity Section -->
  <section class="marketplace-section">
    <div class="section-container">
      <div class="marketplace-header-row">
        <div>
          <h2 class="section-heading">
            {{ auth.isMarketplaceUser() ? 'My Created Services & Job Demands' : 'Ongoing Services & Job Demands' }}
          </h2>
          <p class="section-subheading">
            {{ auth.isMarketplaceUser() 
              ? 'Manage all the ongoing service listings and custom job demands created under your account' 
              : 'Explore active service offerings from providers and custom job requests from receivers' }}
          </p>
        </div>

        <!-- Filter Tabs -->
        <div class="tabs-control">
          <button type="button" class="tab-btn" [class.active]="activeTab() === 'all'" (click)="setTab('all')">
            {{ auth.isMarketplaceUser() ? 'All Created (' + totalCreatedCount() + ')' : 'All Activity' }}
          </button>
          <button type="button" class="tab-btn" [class.active]="activeTab() === 'services'" (click)="setTab('services')">
            {{ auth.isMarketplaceUser() ? 'My Services (' + filteredServices().length + ')' : 'Services (' + filteredServices().length + ')' }}
          </button>
          <button type="button" class="tab-btn" [class.active]="activeTab() === 'jobs'" (click)="setTab('jobs')">
            {{ auth.isMarketplaceUser() ? 'My Jobs (' + filteredJobs().length + ')' : 'Jobs (' + filteredJobs().length + ')' }}
          </button>
        </div>
      </div>

      <!-- Category Filter Chips -->
      <div class="category-filters-row">
        <button type="button" 
                class="cat-chip" 
                [class.active]="selectedCategory() === null" 
                (click)="selectCategory(null)">
          All Categories
        </button>
        @for (cat of categories(); track cat.id) {
          <button type="button" 
                  class="cat-chip" 
                  [class.active]="selectedCategory() === cat.id" 
                  (click)="selectCategory(cat.id)">
            {{ cat.name }}
          </button>
        }
      </div>

      <!-- Services & Jobs Content -->
      @if (isLoadingMarketplace()) {
        <div class="loading-state">
          <div class="spinner"></div>
          <span>Loading listings...</span>
        </div>
      } @else {
        <!-- Global Empty State when Role ID = 1 has created 0 services and 0 jobs -->
        @if (auth.isMarketplaceUser() && activeTab() === 'all' && filteredServices().length === 0 && filteredJobs().length === 0) {
          <div class="empty-state-card card-glass">
            <div class="empty-state-icon">📋</div>
            <h4 class="empty-state-title">No ongoing jobs or services</h4>
            <p class="empty-state-desc">You have not created any services or job requests yet. Offer your first service as a provider or post a job request as a receiver.</p>
            <div class="empty-state-actions">
              <a routerLink="/provider" class="btn btn-outline btn-sm">Offer a Service</a>
              <a routerLink="/receiver" class="btn btn-primary btn-sm">Post a Job Request</a>
            </div>
          </div>
        } @else {
          <!-- SERVICES GRID -->
          @if (activeTab() === 'all' || activeTab() === 'services') {
            <div class="listings-group">
              <div class="group-title-row">
                <h3 class="group-title">
                  {{ auth.isMarketplaceUser() ? '🛠️ My Created Services' : '🛠️ Services Offered by Providers' }}
                </h3>
                @if (auth.isMarketplaceUser()) {
                  <a routerLink="/provider" class="see-all-link">Offer a Service →</a>
                }
              </div>

              @if (filteredServices().length === 0) {
                <div class="empty-state">
                  @if (auth.isMarketplaceUser()) {
                    <span>No ongoing services created yet.</span>
                    <a routerLink="/provider" class="empty-action-link">Offer a Service now →</a>
                  } @else {
                    <span>No active provider services match the filter.</span>
                  }
                </div>
              } @else {
                <div class="cards-grid">
                  @for (item of filteredServices(); track item.id) {
                    <div class="card-glass card-hoverable item-card">
                      <div class="item-card-top">
                        <span class="badge badge-secondary">{{ item.categoryName }}</span>
                        @if (item.isVerified) {
                          <span class="badge badge-verified">✓ Verified</span>
                        }
                      </div>

                      <h4 class="item-title">{{ item.variantName }}</h4>
                      <p class="provider-sub">{{ item.businessName || item.providerName }}</p>

                      @if (item.description) {
                        <p class="item-desc">{{ item.description }}</p>
                      }

                      <div class="item-meta-row">
                        <span class="rating-badge">★ {{ item.ratingAverage.toFixed(1) }} ({{ item.reviewCount }})</span>
                        <span class="areas-badge">📍 {{ item.serviceAreas.join(', ') || 'Islandwide' }}</span>
                      </div>

                      <div class="item-card-bottom">
                        <div class="price-box">
                          <span class="price-val">Rs. {{ item.startingPrice.toLocaleString() }}</span>
                          <span class="price-unit">/ {{ item.priceUnit }}</span>
                        </div>
                        @if (auth.isMarketplaceUser()) {
                          <a routerLink="/provider" class="btn btn-outline btn-sm">Manage</a>
                        } @else {
                          <button type="button" class="btn btn-outline btn-sm">Contact</button>
                        }
                      </div>
                    </div>
                  }
                </div>
              }
            </div>
          }

          <!-- JOBS GRID -->
          @if (activeTab() === 'all' || activeTab() === 'jobs') {
            <div class="listings-group mt-5">
              <div class="group-title-row">
                <h3 class="group-title">
                  {{ auth.isMarketplaceUser() ? '📋 My Posted Job Requests' : '📋 Job Requests Posted by Receivers' }}
                </h3>
                @if (auth.isMarketplaceUser()) {
                  <a routerLink="/receiver" class="see-all-link">Post a Job Request →</a>
                }
              </div>

              @if (filteredJobs().length === 0) {
                <div class="empty-state">
                  @if (auth.isMarketplaceUser()) {
                    <span>No ongoing job requests created yet.</span>
                    <a routerLink="/receiver" class="empty-action-link">Post a Job Request now →</a>
                  } @else {
                    <span>No open receiver jobs match the filter.</span>
                  }
                </div>
              } @else {
                <div class="cards-grid">
                  @for (job of filteredJobs(); track job.id) {
                    <div class="card-glass card-hoverable item-card job-card">
                      <div class="item-card-top">
                        <span class="badge badge-warning">Status: {{ job.status }}</span>
                        @if (job.urgencyOrPreferredDate) {
                          <span class="urgency-badge">⚡ {{ job.urgencyOrPreferredDate }}</span>
                        }
                      </div>

                      <h4 class="item-title">{{ job.title }}</h4>
                      <p class="provider-sub">Posted by {{ job.receiverName }}</p>

                      @if (job.description) {
                        <p class="item-desc">{{ job.description }}</p>
                      }

                      <div class="item-meta-row">
                        @if (job.jobAreas.length > 0) {
                          <span class="areas-badge">📍 {{ job.jobAreas[0].cityName }}</span>
                        }
                        <span class="payment-badge">💳 {{ job.paymentMethod }}</span>
                      </div>

                      <div class="item-card-bottom">
                        <div class="budget-box">
                          <span class="budget-label">Budget</span>
                          <span class="budget-val">Rs. {{ job.budget ? job.budget.toLocaleString() : 'Negotiable' }}</span>
                        </div>
                        @if (auth.isMarketplaceUser()) {
                          <a routerLink="/receiver" class="btn btn-primary btn-sm">Manage</a>
                        } @else {
                          <button type="button" class="btn btn-primary btn-sm">Send Proposal</button>
                        }
                      </div>
                    </div>
                  }
                </div>
              }
            </div>
          }
        }
      }
    </div>
  </section>
</div>
`;
writeFileBoth('src/app/features/dashboard/dashboard.html', dashboardHtml);

// 3. Update features/dashboard/dashboard.css
let dashboardCss = fs.readFileSync(path.join(uiDir, 'src/app/features/dashboard/dashboard.css'), 'utf8');

const extraCss = `
.empty-state-card {
  text-align: center;
  padding: 3.5rem 2rem;
  margin: 1.5rem 0;
  border: 1px dashed var(--border-subtle);
  border-radius: var(--radius-xl);
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.75rem;
}

.empty-state-icon {
  font-size: 2.5rem;
  margin-bottom: 0.25rem;
}

.empty-state-title {
  font-size: 1.25rem;
  font-weight: 700;
  color: var(--text-main);
  margin: 0;
}

.empty-state-desc {
  font-size: 0.9rem;
  color: var(--text-muted);
  max-width: 440px;
  margin: 0;
  line-height: 1.5;
}

.empty-state-actions {
  display: flex;
  gap: 1rem;
  margin-top: 1rem;
  flex-wrap: wrap;
  justify-content: center;
}

.empty-action-link {
  display: inline-block;
  margin-left: 0.5rem;
  color: #38bdf8;
  font-weight: 600;
  text-decoration: underline;
  transition: color 0.2s ease;
}

.empty-action-link:hover {
  color: #7dd3fc;
}
`;

if (!dashboardCss.includes('.empty-state-card')) {
  dashboardCss += extraCss;
  writeFileBoth('src/app/features/dashboard/dashboard.css', dashboardCss);
}

console.log('Dashboard updated successfully!');
