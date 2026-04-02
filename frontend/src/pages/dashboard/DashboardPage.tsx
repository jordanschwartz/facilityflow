import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { DragDropContext, Droppable, Draggable } from '@hello-pangea/dnd';
import type { DropResult } from '@hello-pangea/dnd';
import { useNavigate } from 'react-router-dom';
import { dashboardApi } from '../../api/dashboard';
import { serviceRequestsApi } from '../../api/serviceRequests';
import type { ServiceRequestStatus, ServiceRequestSummary } from '../../types';
import LoadingSpinner from '../../components/ui/LoadingSpinner';
import PriorityBadge from '../../components/ui/PriorityBadge';
import { formatRelativeTime } from '../../utils/formatters';
import toast from 'react-hot-toast';
import {
  ClipboardDocumentListIcon,
  ChatBubbleLeftRightIcon,
  ClockIcon,
  CheckCircleIcon,
} from '@heroicons/react/24/outline';

const PIPELINE_STAGES: ServiceRequestStatus[] = [
  'New', 'Sourcing', 'Quoting', 'PendingApproval', 'Approved', 'Rejected', 'Completed',
];

const STAGE_TAB: Partial<Record<ServiceRequestStatus, string>> = {
  Sourcing: 'vendors',
  Quoting: 'quotes',
  PendingApproval: 'proposal',
  Approved: 'workorder',
  Completed: 'workorder',
};

const STAGE_LABELS: Record<string, string> = {
  New: 'New',
  Sourcing: 'Sourcing',
  Quoting: 'Quoting',
  PendingApproval: 'Pending Approval',
  Approved: 'Approved',
  Rejected: 'Rejected',
  Completed: 'Completed',
};

const STAGE_COLORS: Record<string, string> = {
  New: 'bg-gray-100 text-gray-700',
  Sourcing: 'bg-blue-100 text-blue-700',
  Quoting: 'bg-yellow-100 text-yellow-700',
  PendingApproval: 'bg-purple-100 text-purple-700',
  Approved: 'bg-green-100 text-green-700',
  Rejected: 'bg-red-100 text-red-700',
  Completed: 'bg-emerald-100 text-emerald-700',
};

export default function DashboardPage() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const { data, isLoading } = useQuery({
    queryKey: ['dashboard', 'pipeline'],
    queryFn: () => dashboardApi.getPipeline().then(r => r.data),
  });

  const updateStatus = useMutation({
    mutationFn: ({ id, status }: { id: string; status: ServiceRequestStatus }) =>
      serviceRequestsApi.updateStatus(id, status),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['dashboard', 'pipeline'] });
    },
    onError: () => {
      toast.error('Failed to update status');
      queryClient.invalidateQueries({ queryKey: ['dashboard', 'pipeline'] });
    },
  });

  const onDragEnd = (result: DropResult) => {
    if (!result.destination) return;
    const sourceCol = result.source.droppableId as ServiceRequestStatus;
    const destCol = result.destination.droppableId as ServiceRequestStatus;
    if (sourceCol === destCol) return;

    const item = data?.columns[sourceCol]?.items[result.source.index];
    if (!item) return;

    updateStatus.mutate({ id: item.id, status: destCol });
  };

  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-64">
        <LoadingSpinner />
      </div>
    );
  }

  const stats = data?.stats;

  const statCards = [
    { label: 'Total Open Requests', value: stats?.totalOpenRequests ?? 0, icon: ClipboardDocumentListIcon, color: 'text-blue-600', bg: 'bg-blue-50' },
    { label: 'Pending Quotes', value: stats?.pendingQuotes ?? 0, icon: ChatBubbleLeftRightIcon, color: 'text-yellow-600', bg: 'bg-yellow-50' },
    { label: 'Awaiting Approval', value: stats?.awaitingApproval ?? 0, icon: ClockIcon, color: 'text-purple-600', bg: 'bg-purple-50' },
    { label: 'Completed This Month', value: stats?.completedThisMonth ?? 0, icon: CheckCircleIcon, color: 'text-emerald-600', bg: 'bg-emerald-50' },
  ];

  return (
    <div>
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-gray-900">Dashboard</h1>
        <p className="mt-1 text-sm text-gray-500">Overview of your service pipeline</p>
      </div>

      {/* Stats */}
      <div className="grid grid-cols-4 gap-4 mb-8">
        {statCards.map(({ label, value, icon: Icon, color, bg }) => (
          <div key={label} className="bg-white rounded-xl border border-gray-200 shadow-sm p-5">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-xs font-medium text-gray-500 uppercase tracking-wider">{label}</p>
                <p className="mt-2 text-3xl font-bold text-gray-900">{value}</p>
              </div>
              <div className={`${bg} p-3 rounded-lg`}>
                <Icon className={`w-6 h-6 ${color}`} />
              </div>
            </div>
          </div>
        ))}
      </div>

      {/* Kanban */}
      <DragDropContext onDragEnd={onDragEnd}>
        <div className="flex gap-4 overflow-x-auto pb-4">
          {PIPELINE_STAGES.map((stage) => {
            const col = data?.columns[stage];
            const items: ServiceRequestSummary[] = col?.items ?? [];
            const count = col?.count ?? 0;
            return (
              <div key={stage} className="flex-shrink-0 w-72">
                <div className="flex items-center justify-between mb-3">
                  <h3 className="text-sm font-semibold text-gray-700">{STAGE_LABELS[stage]}</h3>
                  <span className={`inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium ${STAGE_COLORS[stage]}`}>
                    {count}
                  </span>
                </div>
                <Droppable droppableId={stage}>
                  {(provided, snapshot) => (
                    <div
                      ref={provided.innerRef}
                      {...provided.droppableProps}
                      className={`min-h-32 rounded-xl p-2 space-y-2 transition-colors ${
                        snapshot.isDraggingOver ? 'bg-brand-50 border-2 border-brand-200' : 'bg-gray-100'
                      }`}
                    >
                      {items.map((item, index) => (
                        <Draggable key={item.id} draggableId={item.id} index={index}>
                          {(provided, snapshot) => (
                            <div
                              ref={provided.innerRef}
                              {...provided.draggableProps}
                              {...provided.dragHandleProps}
                              onClick={() => {
                                const tab = STAGE_TAB[item.status];
                                navigate(`/requests/${item.id}${tab ? `?tab=${tab}` : ''}`);
                              }}
                              className={`bg-white rounded-lg border p-3 cursor-pointer hover:border-brand-300 transition-all ${
                                snapshot.isDragging ? 'shadow-lg border-brand-300' : 'border-gray-200 shadow-sm'
                              }`}
                            >
                              <p className="text-sm font-medium text-gray-900 line-clamp-2 mb-2">{item.title}</p>
                              <p className="text-xs text-gray-500 mb-2">{item.client?.companyName}</p>
                              <div className="flex items-center justify-between">
                                <PriorityBadge priority={item.priority} />
                                <span className="text-xs text-gray-400">{formatRelativeTime(item.createdAt)}</span>
                              </div>
                              {item.quoteCount > 0 && (
                                <div className="mt-2 text-xs text-gray-500">
                                  {item.quoteCount} quote{item.quoteCount !== 1 ? 's' : ''}
                                </div>
                              )}
                            </div>
                          )}
                        </Draggable>
                      ))}
                      {provided.placeholder}
                    </div>
                  )}
                </Droppable>
              </div>
            );
          })}
        </div>
      </DragDropContext>
    </div>
  );
}
