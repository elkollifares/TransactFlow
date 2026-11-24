import { CommonModule } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule, NgForm } from '@angular/forms';
import { forkJoin } from 'rxjs';
import { Transaction, TransactionRequest, TransactionSummary, TransactionType } from './models';
import { TransactionService } from './transaction.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css'
})
export class AppComponent {
  private readonly service = inject(TransactionService);

  readonly transactions = signal<Transaction[]>([]);
  readonly summary = signal<TransactionSummary | null>(null);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly panelOpen = signal(false);
  readonly editingId = signal<string | null>(null);
  readonly error = signal('');
  readonly toast = signal('');
  readonly search = signal('');
  readonly typeFilter = signal<TransactionType | ''>('');
  readonly todayLabel = new Intl.DateTimeFormat('fr-CA', { weekday: 'long', day: 'numeric', month: 'long' }).format(new Date());
  readonly maxCategory = computed(() => Math.max(...(this.summary()?.expensesByCategory.map(item => item.amount) ?? [1])));

  formModel: TransactionRequest = this.emptyForm();
  private searchTimer?: ReturnType<typeof setTimeout>;

  constructor() {
    this.refresh();
  }

  refresh(): void {
    this.loading.set(true);
    this.error.set('');
    forkJoin({
      transactions: this.service.list(this.search(), this.typeFilter()),
      summary: this.service.summary()
    }).subscribe({
      next: result => {
        this.transactions.set(result.transactions);
        this.summary.set(result.summary);
        this.loading.set(false);
      },
      error: () => {
        this.error.set("Impossible de joindre l’API. Vérifiez qu’elle fonctionne sur le port 5090.");
        this.loading.set(false);
      }
    });
  }

  onSearch(value: string): void {
    this.search.set(value);
    clearTimeout(this.searchTimer);
    this.searchTimer = setTimeout(() => this.loadTransactions(), 250);
  }

  onTypeFilter(value: string): void {
    this.typeFilter.set(value as TransactionType | '');
    this.loadTransactions();
  }

  openCreate(): void {
    this.editingId.set(null);
    this.formModel = this.emptyForm();
    this.panelOpen.set(true);
  }

  openEdit(transaction: Transaction): void {
    this.editingId.set(transaction.id);
    this.formModel = {
      description: transaction.description,
      amount: transaction.amount,
      type: transaction.type,
      category: transaction.category,
      date: transaction.date,
      note: transaction.note ?? ''
    };
    this.panelOpen.set(true);
  }

  closePanel(): void {
    this.panelOpen.set(false);
  }

  save(form: NgForm): void {
    if (form.invalid || this.saving()) return;
    this.saving.set(true);
    const request = { ...this.formModel, amount: Number(this.formModel.amount) };
    const action = this.editingId()
      ? this.service.update(this.editingId()!, request)
      : this.service.create(request);

    action.subscribe({
      next: () => {
        this.saving.set(false);
        this.panelOpen.set(false);
        this.showToast(this.editingId() ? 'Transaction mise à jour' : 'Transaction ajoutée');
        this.refresh();
      },
      error: () => {
        this.saving.set(false);
        this.error.set("La transaction n’a pas pu être enregistrée.");
      }
    });
  }

  remove(transaction: Transaction): void {
    if (!window.confirm(`Supprimer « ${transaction.description} » ?`)) return;
    this.service.delete(transaction.id).subscribe({
      next: () => {
        this.showToast('Transaction supprimée');
        this.refresh();
      },
      error: () => this.error.set("La transaction n’a pas pu être supprimée.")
    });
  }

  runProcedure(): void {
    this.loading.set(true);
    this.service.recalculate().subscribe({
      next: summary => {
        this.summary.set(summary);
        this.loading.set(false);
        this.showToast('Totaux recalculés avec succès');
      },
      error: () => {
        this.loading.set(false);
        this.error.set("Le recalcul n’a pas pu être exécuté.");
      }
    });
  }

  money(value: number): string {
    return new Intl.NumberFormat('fr-CA', { style: 'currency', currency: 'CAD', maximumFractionDigits: 2 }).format(value);
  }

  date(value: string): string {
    return new Intl.DateTimeFormat('fr-CA', { day: '2-digit', month: 'short', year: 'numeric' })
      .format(new Date(`${value}T12:00:00`));
  }

  categoryWidth(amount: number): number {
    return Math.max(6, (amount / this.maxCategory()) * 100);
  }

  trackById(_index: number, transaction: Transaction): string {
    return transaction.id;
  }

  private loadTransactions(): void {
    this.service.list(this.search(), this.typeFilter()).subscribe({
      next: items => this.transactions.set(items),
      error: () => this.error.set("Impossible d’actualiser les transactions.")
    });
  }

  private showToast(message: string): void {
    this.toast.set(message);
    setTimeout(() => this.toast.set(''), 2600);
  }

  private emptyForm(): TransactionRequest {
    return {
      description: '', amount: 0, type: 'Expense', category: '',
      date: new Date().toISOString().slice(0, 10), note: ''
    };
  }
}

