import React from 'react';

interface MetricCardProps {
  title: string;
  subtitle?: string;
  value: string | number;
  icon: React.ReactNode;
  bgColor: string;
}

export const MetricCard: React.FC<MetricCardProps> = ({
  title,
  subtitle,
  value,
  icon,
  bgColor,
}) => {
  return (
    <div className="bg-white dark:bg-gray-800 rounded-lg shadow p-6">
      <div className="flex items-center">
        <div className={`flex items-center justify-center w-12 h-12 rounded-lg ${bgColor}`}>
          {icon}
        </div>
        <div className="ml-4 flex-1">
          <div className="flex items-baseline">
            <p className="text-2xl font-semibold text-gray-900 dark:text-white">
              {value.toLocaleString()}
            </p>
          </div>
          <div className="flex items-center text-sm text-gray-500 dark:text-gray-400">
            <span>{title}</span>
            {subtitle && <span className="ml-1">{subtitle}</span>}
          </div>
        </div>
      </div>
    </div>
  );
};