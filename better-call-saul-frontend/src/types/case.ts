export interface Case {
  id: string;
  caseNumber: string;
  title: string;
  description?: string;
  status: CaseStatus;
  type: CaseType;
  court?: string;
  judge?: string;
  filedDate?: string;
  hearingDate?: string;
  trialDate?: string;
  successProbability?: number;
  estimatedValue?: number;
  userId: string;
  createdAt: string;
  updatedAt?: string;
  isDeleted?: boolean;
}

export enum CaseStatus {
  NEW = 'New',
  INVESTIGATION = 'Investigation',
  DISCOVERY = 'Discovery',
  PRETRIAL = 'PreTrial',
  TRIAL = 'Trial',
  SETTLEMENT = 'Settlement',
  CLOSED = 'Closed',
  APPEALED = 'Appealed',
  DISMISSED = 'Dismissed',
}

export enum CaseType {
  CRIMINAL = 'Criminal',
  CIVIL = 'Civil',
  FAMILY = 'Family',
  IMMIGRATION = 'Immigration',
}

export interface CaseStatistics {
  total: number;
  active: number;
  completed: number;
  overdue: number;
  byStatus: {
    [key in CaseStatus]: number;
  };
}

export interface CaseFilters {
  status?: CaseStatus;
  assignedTo?: string;
  search?: string;
}