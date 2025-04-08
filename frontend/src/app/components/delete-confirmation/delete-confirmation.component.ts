import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-delete-confirmation',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div *ngIf="isOpen" class="fixed inset-0 z-50 flex items-center justify-center bg-black/50 backdrop-blur-sm">
      <div class="glass-popup p-6 rounded-xl max-w-md w-full mx-4">
        <div class="flex items-center justify-between mb-4">
          <h3 class="text-xl font-semibold text-gray-800">Delete Deal</h3>
          <button (click)="onClose()" class="text-gray-500 hover:text-gray-700">
            <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        </div>

        <p class="text-gray-600 mb-6">Are you sure you want to delete "{{dealTitle}}"? This action cannot be undone.</p>

        <div class="flex flex-col space-y-3">
          <div class="relative">
            <input
              type="text"
              [(ngModel)]="confirmationText"
              placeholder="Type 'Delete' to confirm"
              class="w-full px-4 py-2 rounded-lg border border-gray-300 focus:outline-none focus:ring-2 focus:ring-red-500/20 focus:border-red-500 bg-white/50 backdrop-blur-sm"
            />
          </div>

          <div class="flex gap-3 mt-4">
            <button
              (click)="onClose()"
              class="glass-btn flex-1 py-2 hover:cursor-pointer transition-all duration-300 cancel-btn"
            >
              Cancel
            </button>
            <button
              (click)="onConfirm()"
              [disabled]="confirmationText !== 'Delete'"
              class="glass-btn flex-1 py-2 hover:cursor-pointer transition-all duration-300 delete-btn"
              [ngClass]="{'opacity-50 cursor-not-allowed': confirmationText !== 'Delete'}"
            >
              Delete Deal
            </button>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .glass-popup {
      background: rgba(255, 255, 255, 0.9);
      backdrop-filter: blur(10px);
      border: 1px solid rgba(255, 255, 255, 0.3);
      box-shadow: 0 8px 32px rgba(0, 0, 0, 0.1);
    }

    .glass-btn {
      display: flex;
      justify-content: center;
      align-items: center;
      font-size: 0.875rem;
      font-weight: 600;
      border-radius: 8px;
      transition: all 0.3s cubic-bezier(0.165, 0.84, 0.44, 1);
      position: relative;
      overflow: hidden;
      letter-spacing: 0.025em;
      backdrop-filter: blur(4px);
      border: 1px solid rgba(255, 255, 255, 0.3);
      box-shadow: 0 4px 12px rgba(0, 0, 0, 0.05);
    }

    .glass-btn:hover {
      transform: translateY(-2px);
      box-shadow: 0 6px 16px rgba(0, 0, 0, 0.1);
    }

    .glass-btn:active {
      transform: translateY(0);
    }

    .cancel-btn {
      color: #4b5563;
      background: rgba(243, 244, 246, 0.7);
      border-color: rgba(229, 231, 235, 0.5);
    }

    .cancel-btn:hover {
      background: rgba(229, 231, 235, 0.8);
      box-shadow: 0 6px 16px rgba(107, 114, 128, 0.1);
    }

    .delete-btn {
      color: #b91c1c;
      background: rgba(254, 226, 226, 0.7);
      border-color: rgba(254, 202, 202, 0.5);
    }

    .delete-btn:hover {
      background: rgba(254, 202, 202, 0.8);
      box-shadow: 0 6px 16px rgba(254, 202, 202, 0.2);
    }

    .delete-btn:active {
      background: rgba(254, 202, 202, 0.9);
    }
  `]
})
export class DeleteConfirmationComponent {
  @Input() isOpen = false;
  @Input() dealTitle = '';
  @Output() close = new EventEmitter<void>();
  @Output() confirm = new EventEmitter<void>();

  confirmationText = '';

  onClose(): void {
    this.confirmationText = '';
    this.close.emit();
  }

  onConfirm(): void {
    if (this.confirmationText === 'Delete') {
      this.confirm.emit();
      this.onClose();
    }
  }
}
