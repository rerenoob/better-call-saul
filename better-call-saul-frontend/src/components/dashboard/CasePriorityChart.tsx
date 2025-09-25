import React from 'react';
import { CaseStatistics, CasePriority } from '../../types/case';

interface CasePriorityChartProps {
  statistics: CaseStatistics;
  isLoading?: boolean;
}

const priorityColors: Record<CasePriority, string> = {
  [CasePriority.LOW]: 'bg-green-500',
  [CasePriority.MEDIUM]: 'bg-yellow-500',
  [CasePriority.HIGH]: 'bg-orange-500',
  [CasePriority.URGENT]: 'bg-red-500',
};

const priorityLabels: Record<CasePriority, string> = {
  [CasePriority.LOW]: 'Low',
  [CasePriority.MEDIUM]: 'Medium',
  [CasePriority.HIGH]: 'High',
  [CasePriority.URGENT]: 'Urgent',
};

export const CasePriorityChart: React.FC<CasePriorityChartProps> = ({ statistics, isLoading }) => {
  if (isLoading) {
    return (
      <div className="bg-white rounded-lg shadow p-6">
        <h3 className="text-lg font-medium text-gray-900 mb-4">Case Priority Distribution</h3>
        <div className="space-y-3 animate-pulse">
          {[1, 2, 3, 4].map(i => (
            <div key={i} className="flex items-center justify-between">
              <div className="h-4 bg-gray-200 rounded w-16"></div>
              <div className="h-4 bg-gray-200 rounded w-8"></div>
              <div className="h-2 bg-gray-200 rounded flex-1 max-w-32"></div>
            </div>
          ))}
        </div>
      </div>
    );
  }

  const total = Object.values(statistics.byPriority).reduce((sum, count) => sum + count, 0);

  return (
    <div className="bg-white rounded-lg shadow p-6">
      <h3 className="text-lg font-medium text-gray-900 mb-4">Case Priority Distribution</h3>

      <div className="space-y-3">
        {Object.entries(statistics.byPriority).map(([priority, count]) => {
          const percentage = total > 0 ? (count / total) * 100 : 0;

          return (
            <div key={priority} className="flex items-center justify-between">
              <span className="text-sm font-medium text-gray-700 w-16">
                {priorityLabels[priority as CasePriority]}
              </span>

              <span className="text-sm text-gray-600 w-8 text-right">{count}</span>

              <div className="flex-1 max-w-32 ml-2">
                <div className="bg-gray-200 rounded-full h-2">
                  <div
                    className={`h-2 rounded-full ${priorityColors[priority as CasePriority]}`}
                    style={{ width: `${percentage}%` }}
                  />
                </div>
              </div>

              <span className="text-sm text-gray-600 w-12 text-right">
                {percentage.toFixed(0)}%
              </span>
            </div>
          );
        })}
      </div>

      {total === 0 && <div className="text-center py-8 text-gray-500">No case data available</div>}
    </div>
  );
};
