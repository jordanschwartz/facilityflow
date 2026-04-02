import Badge from './Badge';
import { priorityColors } from '../../utils/statusColors';

export default function PriorityBadge({ priority }: { priority: string }) {
  return <Badge label={priority} className={priorityColors[priority] ?? 'bg-gray-100 text-gray-700'} />;
}
