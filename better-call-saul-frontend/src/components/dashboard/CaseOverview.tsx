import React from 'react';
import { CaseStatistics } from '../../types/case';

interface CaseOverviewProps {
  statistics: CaseStatistics;
  isLoading?: boolean;
}

export const CaseOverview: React.FC<CaseOverviewProps> = ({ statistics, isLoading }) => {
  if (isLoading) {
    return (
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-6">
        {[1, 2, 3, 4].map((i) => (
          <div key={i} className="bg-white rounded-lg shadow p-4 animate-pulse">
            <div className="h-4 bg-gray-200 rounded mb-2"></div>
            <div className="h-6 bg-gray-200 rounded"></div>
          </div>
        ))}
      </div>
    );
  }

  const stats = [
    {
      label: 'Total Cases',
      value: statistics.total,
      color: 'bg-blue-500',
    },
    {
      label: 'Active Cases',
      value: statistics.active,
      color: 'bg-green-500',
    },
    {
      label: 'Completed',
      value: statistics.completed,
      color: 'bg-purple-500',
    },
    {
      label: 'Overdue',
      value: statistics.overdue,
      color: 'bg-red-500',
    },
  ];

  return (
    <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-6">
      {stats.map((stat) => (
        <div key={stat.label} className="bg-white rounded-lg shadow p-4">
          <div className="text-sm text-gray-600 mb-1">{stat.label}</div>
          <div className="text-2xl font-bold text-gray-900">{stat.value}</div>
          <div className={`w-8 h-1 ${stat.color} rounded mt-2`}></div>
        </div>
      ))}
    </div>
  );
};