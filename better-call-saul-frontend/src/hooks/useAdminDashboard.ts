import { useState, useEffect } from 'react';
import { adminService, type DashboardMetrics } from '../services/adminService';

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

export interface AdminDashboardData {
  metrics: DashboardMetrics;
  userActivity: UserActivity[];
  isLoading: boolean;
  error: string | null;
}

export const useAdminDashboard = (): AdminDashboardData => {
  const [metrics, setMetrics] = useState<DashboardMetrics>({
    totalUsers: 0,
    activeUsers: 0,
    casesAnalyzed24h: 0,
    avgAnalysisTime: 0,
    activeIncidents: 0,
  });

  const [userActivity, setUserActivity] = useState<UserActivity[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchDashboardData = async () => {
      try {
        setIsLoading(true);

        // Fetch real metrics from API
        const dashboardMetrics = await adminService.getDashboardMetrics();

        // For now, keep mock user activity (can be replaced with real API later)
        const mockUserActivity: UserActivity[] = [
          {
            id: '1',
            user: { name: 'Sarah Johnson', initials: 'SJ' },
            action: 'Uploaded case files',
            time: '2 hours ago',
            type: 'upload',
          },
          {
            id: '2',
            user: { name: 'Michael Chen', initials: 'MC' },
            action: 'Completed analysis',
            time: '3 hours ago',
            type: 'analysis',
          },
          {
            id: '3',
            user: { name: 'Emily Rodriguez', initials: 'ER' },
            action: 'Started new case',
            time: '4 hours ago',
            type: 'upload',
          },
          {
            id: '4',
            user: { name: 'David Kim', initials: 'DK' },
            action: 'Viewed dashboard',
            time: '5 hours ago',
            type: 'other',
          },
          {
            id: '5',
            user: { name: 'Lisa Wang', initials: 'LW' },
            action: 'Updated profile',
            time: '6 hours ago',
            type: 'other',
          },
        ];

        setMetrics(dashboardMetrics);
        setUserActivity(mockUserActivity);
        setError(null);
      } catch (err) {
        setError('Failed to load dashboard data');
        console.error('Error fetching dashboard data:', err);
      } finally {
        setIsLoading(false);
      }
    };

    fetchDashboardData();
  }, []);

  return {
    metrics,
    userActivity,
    isLoading,
    error,
  };
};
