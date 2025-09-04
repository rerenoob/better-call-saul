export interface Case {
  id: string;
  title: string;
  description: string;
  status: CaseStatus;
  priority: CasePriority;
  assignedTo: string;
  createdAt: string;
  updatedAt: string;
  dueDate?: string;
  tags: string[];
}

export enum CaseStatus {
  DRAFT = 'DRAFT',
  ACTIVE = 'ACTIVE',
  REVIEW = 'REVIEW',
  COMPLETED = 'COMPLETED',
  ARCHIVED = 'ARCHIVED',
}

export enum CasePriority {
  LOW = 'LOW',
  MEDIUM = 'MEDIUM',
  HIGH = 'HIGH',
  URGENT = 'URGENT',
}

export interface CaseStatistics {
  total: number;
  active: number;
  completed: number;
  overdue: number;
  byPriority: {
    [key in CasePriority]: number;
  };
  byStatus: {
    [key in CaseStatus]: number;
  };
}

export interface CaseFilters {
  status?: CaseStatus;
  priority?: CasePriority;
  assignedTo?: string;
  search?: string;
}