import React from 'react';

export interface UserActivity {
  id: string;
  user: {
    name: string;
    initials: string;
  };
  action: string;
  time: string;
  type: 'upload' | 'login' | 'upgrade' | 'analysis' | 'other';
}

interface UserActivityFeedProps {
  activities: UserActivity[];
}

const getActivityColor = (type: UserActivity['type']) => {
  switch (type) {
    case 'upload':
      return 'bg-blue-100 text-blue-600';
    case 'login':
      return 'bg-green-100 text-green-600';
    case 'upgrade':
      return 'bg-purple-100 text-purple-600';
    case 'analysis':
      return 'bg-orange-100 text-orange-600';
    default:
      return 'bg-gray-100 text-gray-600';
  }
};

export const UserActivityFeed: React.FC<UserActivityFeedProps> = ({ activities }) => {
  if (activities.length === 0) {
    return (
      <div className="p-6 text-center text-gray-500 dark:text-gray-400">
        <p>No recent activity to display.</p>
      </div>
    );
  }

  return (
    <div className="p-6">
      <div className="flow-root">
        <ul className="-mb-8">
          {activities.map((activity, index) => (
            <li key={activity.id}>
              <div className="relative pb-8">
                {index !== activities.length - 1 ? (
                  <span
                    className="absolute top-4 left-4 -ml-px h-full w-0.5 bg-gray-200 dark:bg-gray-700"
                    aria-hidden="true"
                  />
                ) : null}
                <div className="relative flex space-x-3">
                  <div>
                    <span
                      className={`h-8 w-8 rounded-full flex items-center justify-center text-sm font-medium ${getActivityColor(activity.type)}`}
                    >
                      {activity.user.initials}
                    </span>
                  </div>
                  <div className="flex-1 min-w-0">
                    <div>
                      <p className="text-sm text-gray-900 dark:text-white">
                        <span className="font-medium">{activity.user.name}</span>{' '}
                        {activity.action}
                      </p>
                      <p className="mt-0.5 text-sm text-gray-500 dark:text-gray-400">
                        {activity.time}
                      </p>
                    </div>
                  </div>
                </div>
              </div>
            </li>
          ))}
        </ul>
      </div>
    </div>
  );
};