import React from 'react';
import { Case, CaseStatus, CasePriority } from '../../types/case';

interface RecentCasesProps {
  cases: Case[];
  isLoading?: boolean;
}

const statusColors: Record<CaseStatus, string> = {
  [CaseStatus.NEW]: 'bg-gray-100 text-gray-800',
  [CaseStatus.INVESTIGATION]: 'bg-blue-100 text-blue-800',
  [CaseStatus.DISCOVERY]: 'bg-yellow-100 text-yellow-800',
  [CaseStatus.PRETRIAL]: 'bg-orange-100 text-orange-800',
  [CaseStatus.TRIAL]: 'bg-red-100 text-red-800',
  [CaseStatus.SETTLEMENT]: 'bg-green-100 text-green-800',
  [CaseStatus.CLOSED]: 'bg-purple-100 text-purple-800',
  [CaseStatus.APPEALED]: 'bg-indigo-100 text-indigo-800',
  [CaseStatus.DISMISSED]: 'bg-pink-100 text-pink-800',
};

const priorityColors: Record<CasePriority, string> = {
  [CasePriority.LOW]: 'bg-green-100 text-green-800',
  [CasePriority.MEDIUM]: 'bg-yellow-100 text-yellow-800',
  [CasePriority.HIGH]: 'bg-orange-100 text-orange-800',
  [CasePriority.URGENT]: 'bg-red-100 text-red-800',
};

export const RecentCases: React.FC<RecentCasesProps> = ({ cases, isLoading }) => {
  if (isLoading) {
    return (
      <div className="bg-white rounded-lg shadow p-6">
        <h3 className="text-lg font-medium text-gray-900 mb-4">Recent Cases</h3>
        <div className="space-y-3">
          {[1, 2, 3, 4, 5].map((i) => (
            <div key={i} className="flex items-center justify-between p-3 border rounded animate-pulse">
              <div className="flex items-center space-x-3">
                <div className="h-4 bg-gray-200 rounded w-24"></div>
                <div className="h-4 bg-gray-200 rounded w-32"></div>
              </div>
              <div className="h-6 bg-gray-200 rounded w-16"></div>
            </div>
          ))}
        </div>
      </div>
    );
  }

  return (
    <div className="bg-white rounded-lg shadow p-6">
      <div className="flex items-center justify-between mb-4">
        <h3 className="text-lg font-medium text-gray-900">Recent Cases</h3>
        <span className="text-sm text-gray-500">{cases.length} cases</span>
      </div>

      <div className="space-y-3">
        {cases.map((caseItem) => (
          <div key={caseItem.id} className="flex items-center justify-between p-3 border rounded hover:bg-gray-50">
            <div className="flex items-center space-x-3">
              <div className="flex-shrink-0">
                <div className={`px-2 py-1 text-xs font-medium rounded-full ${priorityColors[caseItem.priority]}`}>
                  {caseItem.priority}
                </div>
              </div>
              <div>
                <h4 className="text-sm font-medium text-gray-900">{caseItem.title}</h4>
                <p className="text-xs text-gray-500">{caseItem.description ? `${caseItem.description.substring(0, 50)}...` : 'No description'}</p>
              </div>
            </div>
            <div className={`px-2 py-1 text-xs font-medium rounded-full ${statusColors[caseItem.status]}`}>
              {caseItem.status}
            </div>
          </div>
        ))}
      </div>

      {cases.length === 0 && (
        <div className="text-center py-8 text-gray-500">
          No cases found
        </div>
      )}
    </div>
  );
};