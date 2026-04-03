import { useState } from 'react';
import { NavLink, useNavigate } from 'react-router-dom';
import { HomeIcon, WrenchScrewdriverIcon, BuildingOfficeIcon, UserGroupIcon, BanknotesIcon, UsersIcon, Cog6ToothIcon, ArrowRightOnRectangleIcon, ChevronLeftIcon, ChevronRightIcon } from '@heroicons/react/24/outline';
import { useAuthStore } from '../../stores/authStore';

const navItems = [
  { to: '/dashboard', label: 'Dashboard', icon: HomeIcon },
  { to: '/work-orders', label: 'Work Orders', icon: WrenchScrewdriverIcon },
  { to: '/invoices', label: 'Invoices', icon: BanknotesIcon },
  { to: '/vendors', label: 'Vendors', icon: BuildingOfficeIcon },
  { to: '/clients', label: 'Clients', icon: UserGroupIcon },
];

export default function Sidebar() {
  const { user, clearAuth } = useAuthStore();
  const navigate = useNavigate();
  const isAdmin = user?.role === 'Operator' || user?.isAdmin;
  const [collapsed, setCollapsed] = useState(true);

  return (
    <div className={`flex flex-col bg-gray-900 min-h-screen transition-all duration-200 ${collapsed ? 'w-16' : 'w-64'}`}>
      <div className={`flex items-center h-16 border-b border-gray-700 ${collapsed ? 'justify-center px-2' : 'gap-3 px-6'}`}>
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 48 28" className="w-10 h-6 flex-shrink-0">
          <rect width="48" height="28" rx="14" fill="#E8511A"/>
          <circle cx="34" cy="14" r="10" fill="white"/>
        </svg>
        {!collapsed && (
          <div>
            <div className="text-white font-bold text-lg leading-none">On-Call</div>
            <div className="text-gray-400 text-[9px] font-semibold tracking-widest uppercase mt-0.5">Facilities & Maintenance</div>
          </div>
        )}
      </div>
      <nav className="flex-1 px-2 py-4 space-y-1">
        {navItems.map(({ to, label, icon: Icon }) => (
          <NavLink
            key={to}
            to={to}
            title={collapsed ? label : undefined}
            className={({ isActive }) =>
              `flex items-center rounded-lg text-sm font-medium transition-colors ${
                collapsed ? 'justify-center px-0 py-2.5' : 'gap-3 px-3 py-2.5'
              } ${isActive ? 'bg-brand-600 text-white' : 'text-gray-400 hover:bg-gray-800 hover:text-white'}`
            }
          >
            <Icon className="w-5 h-5 flex-shrink-0" />
            {!collapsed && label}
          </NavLink>
        ))}
        {isAdmin && (
          <NavLink
            to="/admin/users"
            title={collapsed ? 'User Management' : undefined}
            className={({ isActive }) =>
              `flex items-center rounded-lg text-sm font-medium transition-colors ${
                collapsed ? 'justify-center px-0 py-2.5' : 'gap-3 px-3 py-2.5'
              } ${isActive ? 'bg-brand-600 text-white' : 'text-gray-400 hover:bg-gray-800 hover:text-white'}`
            }
          >
            <UsersIcon className="w-5 h-5 flex-shrink-0" />
            {!collapsed && 'User Management'}
          </NavLink>
        )}
      </nav>
      <div className="px-2 py-4 border-t border-gray-700">
        <NavLink
          to="/settings/profile"
          title={collapsed ? 'Settings' : undefined}
          className={({ isActive }) =>
            `flex items-center rounded-lg text-sm font-medium transition-colors mb-1 ${
              collapsed ? 'justify-center px-0 py-2.5' : 'gap-3 px-3 py-2.5'
            } ${isActive ? 'bg-brand-600 text-white' : 'text-gray-400 hover:bg-gray-800 hover:text-white'}`
          }
        >
          <Cog6ToothIcon className="w-5 h-5 flex-shrink-0" />
          {!collapsed && 'Settings'}
        </NavLink>
        {!collapsed && (
          <div className="px-3 py-2 mb-1">
            <p className="text-white text-sm font-medium truncate">{user?.name}</p>
            <p className="text-gray-400 text-xs truncate">{user?.email}</p>
          </div>
        )}
        <button
          onClick={() => { clearAuth(); navigate('/login'); }}
          title={collapsed ? 'Sign Out' : undefined}
          className={`flex items-center w-full text-sm text-gray-400 hover:bg-gray-800 hover:text-white rounded-lg transition-colors ${
            collapsed ? 'justify-center px-0 py-2' : 'gap-3 px-3 py-2'
          }`}
        >
          <ArrowRightOnRectangleIcon className="w-5 h-5 flex-shrink-0" />
          {!collapsed && 'Sign Out'}
        </button>
        <button
          onClick={() => setCollapsed(!collapsed)}
          className={`flex items-center w-full mt-2 text-sm text-gray-500 hover:bg-gray-800 hover:text-white rounded-lg transition-colors ${
            collapsed ? 'justify-center px-0 py-2' : 'gap-3 px-3 py-2'
          }`}
          title={collapsed ? 'Expand sidebar' : 'Collapse sidebar'}
        >
          {collapsed ? <ChevronRightIcon className="w-4 h-4" /> : <><ChevronLeftIcon className="w-4 h-4" /> <span>Collapse</span></>}
        </button>
      </div>
    </div>
  );
}
