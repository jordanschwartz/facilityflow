import Badge from './Badge';
import { statusColors } from '../../utils/statusColors';

export default function StatusBadge({ status }: { status: string }) {
  return <Badge label={status} className={statusColors[status] ?? 'bg-gray-100 text-gray-700'} />;
}
