export type TransactionType = 'Income' | 'Expense';

export interface Transaction {
  id: string;
  description: string;
  amount: number;
  type: TransactionType;
  category: string;
  date: string;
  note?: string;
  createdAt: string;
  updatedAt: string;
}

export interface TransactionRequest {
  description: string;
  amount: number;
  type: TransactionType;
  category: string;
  date: string;
  note?: string;
}

export interface TransactionSummary {
  totalIncome: number;
  totalExpense: number;
  balance: number;
  transactionCount: number;
  expensesByCategory: Array<{ category: string; amount: number }>;
  monthlyTrend: Array<{ month: string; income: number; expense: number }>;
  calculatedAt: string;
  calculationEngine: string;
}

