import { NavLink, useNavigate } from 'react-router-dom';
import { HomeIcon, ClipboardDocumentListIcon, WrenchScrewdriverIcon, BuildingOfficeIcon, UserGroupIcon, ArrowRightOnRectangleIcon } from '@heroicons/react/24/outline';
import { useAuthStore } from '../../stores/authStore';

const navItems = [
  { to: '/dashboard', label: 'Dashboard', icon: HomeIcon },
  { to: '/requests', label: 'Requests', icon: ClipboardDocumentListIcon },
  { to: '/work-orders', label: 'Work Orders', icon: WrenchScrewdriverIcon },
  { to: '/vendors', label: 'Vendors', icon: BuildingOfficeIcon },
  { to: '/clients', label: 'Clients', icon: UserGroupIcon },
];

export default function Sidebar() {
  const { user, clearAuth } = useAuthStore();
  const navigate = useNavigate();

  return (
    <div className="flex flex-col w-64 bg-gray-900 min-h-screen">
      <div className="flex items-center gap-3 px-6 py-5 border-b border-gray-700">
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 48 28" className="w-10 h-6 flex-shrink-0">
          <rect width="48" height="28" rx="14" fill="#E8511A"/>
          <circle cx="34" cy="14" r="10" fill="white"/>
        </svg>
        <div>
          <div className="text-white font-bold text-lg leading-none">On-Call</div>
          <div className="text-gray-400 text-[9px] font-semibold tracking-widest uppercase mt-0.5">Facilities & Maintenance</div>
        </div>
      </div>
      <nav className="flex-1 px-3 py-4 space-y-1">
        {navItems.map(({ to, label, icon: Icon }) => (
          <NavLink
            key={to}
            to={to}
            className={({ isActive }) =>
              `flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm font-medium transition-colors ${
                isActive ? 'bg-brand-600 text-white' : 'text-gray-400 hover:bg-gray-800 hover:text-white'
              }`
            }
          >
            <Icon className="w-5 h-5" />
            {label}
          </NavLink>
        ))}
      </nav>
      <div className="px-3 py-4 border-t border-gray-700">
        <div className="px-3 py-2 mb-1">
          <p className="text-white text-sm font-medium truncate">{user?.name}</p>
          <p className="text-gray-400 text-xs truncate">{user?.email}</p>
        </div>
        <button
          onClick={() => { clearAuth(); navigate('/login'); }}
          className="flex items-center gap-3 w-full px-3 py-2 text-sm text-gray-400 hover:bg-gray-800 hover:text-white rounded-lg transition-colors"
        >
          <ArrowRightOnRectangleIcon className="w-5 h-5" />
          Sign Out
        </button>
      </div>
    </div>
  );
}
