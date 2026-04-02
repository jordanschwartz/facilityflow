import Badge from './Badge';
import { statusColors, statusLabels } from '../../utils/statusColors';

export default function StatusBadge({ status }: { status: string }) {
  return <Badge label={statusLabels[status] ?? status} className={statusColors[status] ?? 'bg-gray-100 text-gray-700'} />;
}
