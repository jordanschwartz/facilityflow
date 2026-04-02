import { BellIcon } from '@heroicons/react/24/outline';
import { useAuthStore } from '../../stores/authStore';

export default function TopBar({ title }: { title?: string }) {
  const { user } = useAuthStore();
  return (
    <div className="h-16 bg-white border-b border-gray-200 flex items-center justify-between px-6">
      <div>{title && <span className="font-semibold text-gray-700">{title}</span>}</div>
      <div className="flex items-center gap-3">
        <button className="relative p-2 text-gray-400 hover:text-gray-600 rounded-lg hover:bg-gray-100">
          <BellIcon className="w-5 h-5" />
        </button>
        <div className="w-8 h-8 rounded-full bg-brand-600 flex items-center justify-center text-white text-sm font-medium">
          {user?.name?.[0]?.toUpperCase()}
        </div>
      </div>
    </div>
  );
}
