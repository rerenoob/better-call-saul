import React, { useState } from 'react';
import { Outlet } from 'react-router-dom';
import { AdminNavigation } from './AdminNavigation';
import { AdminHeader } from './AdminHeader';

export const AdminLayout: React.FC = () => {
  const [sidebarOpen, setSidebarOpen] = useState(false);

  return (
    <div className="min-h-screen bg-gray-100 dark:bg-gray-900 flex">
      {/* Admin Navigation Sidebar */}
      <AdminNavigation isOpen={sidebarOpen} />
      
      {/* Mobile Overlay */}
      {sidebarOpen && (
        <div 
          className="fixed inset-0 z-40 bg-gray-600 bg-opacity-75 lg:hidden"
          onClick={() => setSidebarOpen(false)}
        />
      )}
      
      {/* Main Content Area */}
      <div className={`flex-1 min-w-0 transition-all duration-300 ease-in-out ${
        sidebarOpen ? 'lg:ml-64 ml-64' : 'ml-0'
      }`}>
        {/* Admin Header */}
        <AdminHeader 
          onToggleSidebar={() => setSidebarOpen(!sidebarOpen)}
          sidebarOpen={sidebarOpen}
        />
        
        {/* Page Content */}
        <main className="p-4 sm:p-6 lg:p-8 flex-1">
          <Outlet />
        </main>
      </div>
    </div>
  );
};