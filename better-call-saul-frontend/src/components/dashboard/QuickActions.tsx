import React from 'react';

interface QuickActionsProps {
  onNewCase?: () => void;
  onUploadDocument?: () => void;
  onViewReports?: () => void;
  onSearchCases?: () => void;
}

export const QuickActions: React.FC<QuickActionsProps> = ({
  onNewCase,
  onUploadDocument,
  onViewReports,
  onSearchCases,
}) => {
  const actions = [
    {
      label: 'New Case',
      description: 'Create a new legal case',
      icon: '📋',
      onClick: onNewCase,
      color: 'bg-blue-500 hover:bg-blue-600',
    },
    {
      label: 'Upload Document',
      description: 'Upload case documents',
      icon: '📄',
      onClick: onUploadDocument,
      color: 'bg-green-500 hover:bg-green-600',
    },
    {
      label: 'View Reports',
      description: 'Generate case reports',
      icon: '📊',
      onClick: onViewReports,
      color: 'bg-purple-500 hover:bg-purple-600',
    },
    {
      label: 'Search Cases',
      description: 'Search existing cases',
      icon: '🔍',
      onClick: onSearchCases,
      color: 'bg-orange-500 hover:bg-orange-600',
    },
  ];

  return (
    <div className="bg-white rounded-lg shadow p-6">
      <h3 className="text-lg font-medium text-gray-900 mb-4">Quick Actions</h3>
      
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
        {actions.map((action) => (
          <button
            key={action.label}
            onClick={action.onClick}
            className="flex flex-col items-center p-4 border border-gray-200 rounded-lg hover:shadow-md transition-shadow focus:outline-none focus:ring-2 focus:ring-blue-500"
          >
            <div className="text-2xl mb-2">{action.icon}</div>
            <div className="text-sm font-medium text-gray-900 mb-1">{action.label}</div>
            <div className="text-xs text-gray-500 text-center">{action.description}</div>
          </button>
        ))}
      </div>
    </div>
  );
};