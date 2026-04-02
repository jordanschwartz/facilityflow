import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { DragDropContext, Droppable, Draggable } from '@hello-pangea/dnd';
import type { DropResult } from '@hello-pangea/dnd';
import { useNavigate } from 'react-router-dom';
import { dashboardApi } from '../../api/dashboard';
import { serviceRequestsApi } from '../../api/serviceRequests';
import type { ServiceRequestStatus, ServiceRequestSummary } from '../../types';
import LoadingSpinner from '../../components/ui/LoadingSpinner';
import PriorityBadge from '../../components/ui/PriorityBadge';
import StatusBadge from '../../components/ui/StatusBadge';
import { formatRelativeTime } from '../../utils/formatters';
import toast from 'react-hot-toast';
import { ExclamationTriangleIcon } from '@heroicons/react/24/outline';

interface PipelinePhase {
  id: string;
  label: string;
  color: string;
  statuses: ServiceRequestStatus[];
}

const PIPELINE_PHASES: PipelinePhase[] = [
  {
    id: 'intake',
    label: 'Intake',
    color: 'bg-gray-200',
    statuses: ['New', 'Qualifying'],
  },
  {
    id: 'sourcing',
    label: 'Sourcing',
    color: 'bg-blue-200',
    statuses: ['Sourcing'],
  },
  {
    id: 'scheduling',
    label: 'Scheduling',
    color: 'bg-indigo-200',
    statuses: ['SchedulingSiteVisit', 'ScheduleConfirmed'],
  },
  {
    id: 'quoting',
    label: 'Quoting & Approval',
    color: 'bg-yellow-200',
    statuses: ['PendingQuotes', 'ProposalReady', 'PendingApproval'],
  },
  {
    id: 'po',
    label: 'PO Gate',
    color: 'bg-red-100',
    statuses: ['AwaitingPO', 'POReceived'],
  },
  {
    id: 'execution',
    label: 'Execution',
    color: 'bg-green-100',
    statuses: ['JobInProgress', 'JobCompleted'],
  },
  {
    id: 'closeout',
    label: 'Closeout',
    color: 'bg-emerald-100',
    statuses: ['Verification', 'InvoiceSent', 'InvoicePaid'],
  },
];

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

  // Gather all items for a phase from the pipeline columns
  const getPhaseItems = (phase: PipelinePhase): ServiceRequestSummary[] => {
    if (!data?.columns) return [];
    return phase.statuses.flatMap(status => data.columns[status]?.items ?? []);
  };

  const getPhaseCount = (phase: PipelinePhase): number => {
    if (!data?.columns) return 0;
    return phase.statuses.reduce((sum, status) => sum + (data.columns[status]?.count ?? 0), 0);
  };

  const onDragEnd = (result: DropResult) => {
    if (!result.destination) return;
    const sourcePhaseId = result.source.droppableId;
    const destPhaseId = result.destination.droppableId;
    if (sourcePhaseId === destPhaseId) return;

    // Find the source phase and locate the item
    const sourcePhase = PIPELINE_PHASES.find(p => p.id === sourcePhaseId);
    if (!sourcePhase) return;

    const sourceItems = getPhaseItems(sourcePhase);
    const item = sourceItems[result.source.index];
    if (!item) return;

    // Find the destination phase and set to its first status
    const destPhase = PIPELINE_PHASES.find(p => p.id === destPhaseId);
    if (!destPhase) return;

    const newStatus = destPhase.statuses[0];
    updateStatus.mutate({ id: item.id, status: newStatus });
  };

  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-64">
        <LoadingSpinner />
      </div>
    );
  }

  return (
    <div>
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-gray-900">Operations Command Center</h1>
        <p className="mt-1 text-sm text-gray-500">Work order pipeline overview</p>
      </div>

      {/* Kanban */}
      <DragDropContext onDragEnd={onDragEnd}>
        <div className="grid grid-cols-7 gap-3 min-w-0">
          {PIPELINE_PHASES.map((phase) => {
            const items = getPhaseItems(phase);
            const count = getPhaseCount(phase);
            const isPOGate = phase.id === 'po';
            return (
              <div key={phase.id} className="min-w-0">
                <div className="flex items-center justify-between mb-3">
                  <h3 className="text-sm font-semibold text-gray-700">{phase.label}</h3>
                  <span className={`inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium ${phase.color} ${isPOGate ? 'text-red-700' : 'text-gray-700'}`}>
                    {count}
                  </span>
                </div>
                <Droppable droppableId={phase.id}>
                  {(provided, snapshot) => (
                    <div
                      ref={provided.innerRef}
                      {...provided.droppableProps}
                      className={`min-h-32 rounded-xl p-2 space-y-2 transition-colors ${
                        snapshot.isDraggingOver
                          ? 'bg-brand-50 border-2 border-brand-200'
                          : isPOGate
                            ? 'bg-red-50 border border-red-200'
                            : 'bg-gray-100'
                      }`}
                    >
                      {items.map((item, index) => (
                        <Draggable key={item.id} draggableId={item.id} index={index}>
                          {(provided, snapshot) => (
                            <div
                              ref={provided.innerRef}
                              {...provided.draggableProps}
                              {...provided.dragHandleProps}
                              onClick={() => navigate(`/work-orders/${item.id}`)}
                              className={`bg-white rounded-lg border p-3 cursor-pointer hover:border-brand-300 transition-all ${
                                snapshot.isDragging ? 'shadow-lg border-brand-300' : 'border-gray-200 shadow-sm'
                              }`}
                            >
                              <p className="text-sm font-medium text-gray-900 line-clamp-2 mb-2">{item.title}</p>
                              <p className="text-xs text-gray-500 mb-2">{item.client?.companyName}</p>
                              <div className="flex items-center gap-1.5 flex-wrap mb-2">
                                <StatusBadge status={item.status} />
                                <PriorityBadge priority={item.priority} />
                              </div>
                              <div className="flex items-center justify-between text-xs text-gray-400">
                                <span>{formatRelativeTime(item.updatedAt)}</span>
                                {item.quoteCount > 0 && (
                                  <span className="text-gray-500">
                                    {item.quoteCount} quote{item.quoteCount !== 1 ? 's' : ''}
                                  </span>
                                )}
                              </div>
                              {item.status === 'AwaitingPO' && (
                                <div className="mt-1.5 text-xs font-medium text-red-600 flex items-center gap-1">
                                  <span className="inline-block w-2 h-2 rounded-full bg-red-500" />
                                  Awaiting PO
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
