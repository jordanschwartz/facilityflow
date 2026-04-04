import { useState } from 'react';
import { useParams, Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import toast from 'react-hot-toast';
import { workOrdersApi } from '../../api/workOrders';
import LoadingSpinner from '../../components/ui/LoadingSpinner';
import Button from '../../components/ui/Button';
import { formatDate } from '../../utils/formatters';
import {
  DocumentTextIcon,
  ArrowDownTrayIcon,
  MapPinIcon,
  CalendarDaysIcon,
  EnvelopeIcon,
  UserIcon,
} from '@heroicons/react/24/solid';

export default function WorkOrderViewPage() {
  const { token } = useParams<{ token: string }>();
  const [downloadingPdf, setDownloadingPdf] = useState(false);

  const { data: workOrder, isLoading, error } = useQuery({
    queryKey: ['work-order-view', token],
    queryFn: () => workOrdersApi.getWorkOrderByToken(token!).then(r => r.data),
    enabled: !!token,
    retry: false,
  });

  const handleDownloadPdf = async () => {
    if (!token) return;
    setDownloadingPdf(true);
    try {
      const res = await workOrdersApi.downloadWorkOrderPdf(token);
      const blob = new Blob([res.data], { type: 'application/pdf' });
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `work-order-${workOrder?.workOrderNumber ?? token}.pdf`;
      document.body.appendChild(a);
      a.click();
      document.body.removeChild(a);
      URL.revokeObjectURL(url);
    } catch {
      toast.error('Failed to download PDF');
    } finally {
      setDownloadingPdf(false);
    }
  };

  if (isLoading) {
    return (
      <div className="min-h-screen bg-gray-50 flex items-center justify-center">
        <LoadingSpinner />
      </div>
    );
  }

  if (error || !workOrder) {
    return (
      <div className="min-h-screen bg-gray-50 flex items-center justify-center">
        <div className="text-center max-w-md px-4">
          <div className="w-16 h-16 bg-gray-100 rounded-full flex items-center justify-center mx-auto mb-4">
            <DocumentTextIcon className="w-8 h-8 text-gray-400" />
          </div>
          <h1 className="text-xl font-semibold text-gray-900 mb-2">Work order not found</h1>
          <p className="text-sm text-gray-500">This work order link is not valid or has expired.</p>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gray-50">
      {/* Header */}
      <div className="bg-white border-b border-gray-200">
        <div className="max-w-2xl mx-auto px-4 sm:px-6 py-4">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-2">
              <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 48 28" className="w-8 h-5 flex-shrink-0">
                <rect width="48" height="28" rx="14" fill="#E8511A" />
                <circle cx="34" cy="14" r="10" fill="white" />
              </svg>
              <span className="font-bold text-lg" style={{ color: '#E8511A' }}>On-Call</span>
              <span className="text-gray-400">|</span>
              <span className="text-gray-500 text-sm">Work Order</span>
            </div>
            {workOrder.workOrderNumber && (
              <span className="text-sm font-medium text-gray-500">{workOrder.workOrderNumber}</span>
            )}
          </div>
        </div>
      </div>

      <div className="max-w-2xl mx-auto py-8 sm:py-12 px-4 sm:px-6">
        {/* Greeting */}
        <div className="mb-6">
          <h1 className="text-xl font-bold text-gray-900">
            Hello, {workOrder.vendorName}
          </h1>
          <p className="text-sm text-gray-600 mt-1">
            You have been assigned a work order. Please review the details below.
          </p>
        </div>

        {/* Main Card */}
        <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden mb-6">
          {/* Job Information */}
          <div className="px-6 sm:px-8 pt-8 pb-6">
            <h2 className="text-sm font-semibold text-gray-900 mb-4 uppercase tracking-wider">Job Information</h2>
            <dl className="space-y-4">
              <div>
                <dt className="text-xs font-medium text-gray-500 uppercase tracking-wider">Title</dt>
                <dd className="mt-1 text-sm font-medium text-gray-900">{workOrder.title}</dd>
              </div>
              {workOrder.description && (
                <div>
                  <dt className="text-xs font-medium text-gray-500 uppercase tracking-wider">Description</dt>
                  <dd className="mt-1 text-sm text-gray-700 whitespace-pre-wrap leading-relaxed">{workOrder.description}</dd>
                </div>
              )}
              <div className="grid grid-cols-2 gap-4">
                <div className="flex items-start gap-2">
                  <MapPinIcon className="w-4 h-4 text-gray-400 mt-0.5 flex-shrink-0" />
                  <div>
                    <dt className="text-xs font-medium text-gray-500 uppercase tracking-wider">Location</dt>
                    <dd className="mt-1 text-sm text-gray-900">
                      <a
                        href={`https://www.google.com/maps/search/?api=1&query=${encodeURIComponent(workOrder.serviceLocation)}`}
                        target="_blank"
                        rel="noopener noreferrer"
                        className="text-brand-600 hover:text-brand-700 hover:underline"
                      >
                        {workOrder.serviceLocation}
                      </a>
                    </dd>
                  </div>
                </div>
                <div>
                  <dt className="text-xs font-medium text-gray-500 uppercase tracking-wider">Service</dt>
                  <dd className="mt-1 text-sm text-gray-900">{workOrder.category}</dd>
                </div>
              </div>
              <div>
                <dt className="text-xs font-medium text-gray-500 uppercase tracking-wider">Priority</dt>
                <dd className="mt-1">
                  <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${
                    workOrder.priority === 'Urgent' ? 'bg-red-100 text-red-700' :
                    workOrder.priority === 'High' ? 'bg-orange-100 text-orange-700' :
                    workOrder.priority === 'Medium' ? 'bg-blue-100 text-blue-700' :
                    'bg-gray-100 text-gray-700'
                  }`}>
                    {workOrder.priority}
                  </span>
                </dd>
              </div>
            </dl>
          </div>

          {/* Scheduling */}
          {(workOrder.requestedDate || workOrder.scheduledDate) && (
            <div className="px-6 sm:px-8 pb-6">
              <h2 className="text-sm font-semibold text-gray-900 mb-3 uppercase tracking-wider">Scheduling</h2>
              <div className="grid grid-cols-2 gap-4">
                {workOrder.requestedDate && (
                  <div className="flex items-start gap-3 bg-gray-50 rounded-lg p-3">
                    <CalendarDaysIcon className="w-5 h-5 text-gray-400 flex-shrink-0 mt-0.5" />
                    <div>
                      <p className="text-xs font-medium text-gray-500 uppercase tracking-wider">Requested Date</p>
                      <p className="text-sm font-medium text-gray-900 mt-0.5">{formatDate(workOrder.requestedDate)}</p>
                    </div>
                  </div>
                )}
                {workOrder.scheduledDate && (
                  <div className="flex items-start gap-3 bg-gray-50 rounded-lg p-3">
                    <CalendarDaysIcon className="w-5 h-5 text-gray-400 flex-shrink-0 mt-0.5" />
                    <div>
                      <p className="text-xs font-medium text-gray-500 uppercase tracking-wider">Scheduled Date</p>
                      <p className="text-sm font-medium text-gray-900 mt-0.5">{formatDate(workOrder.scheduledDate)}</p>
                    </div>
                  </div>
                )}
              </div>
            </div>
          )}

          {/* Contact Information */}
          <div className="px-6 sm:px-8 pb-6">
            <h2 className="text-sm font-semibold text-gray-900 mb-3 uppercase tracking-wider">Contact</h2>
            <div className="bg-brand-50 border border-brand-100 rounded-lg p-4 space-y-3">
              <div className="flex items-center gap-3">
                <UserIcon className="w-4 h-4 text-brand-600 flex-shrink-0" />
                <div>
                  <p className="text-xs font-medium text-brand-700 uppercase tracking-wider">Name</p>
                  <p className="text-sm font-medium text-gray-900">{workOrder.contactName}</p>
                </div>
              </div>
              {workOrder.contactEmail && (
                <div className="flex items-center gap-3">
                  <EnvelopeIcon className="w-4 h-4 text-brand-600 flex-shrink-0" />
                  <div>
                    <p className="text-xs font-medium text-brand-700 uppercase tracking-wider">Email</p>
                    <a href={`mailto:${workOrder.contactEmail}`} className="text-sm font-medium text-brand-600 hover:text-brand-700">
                      {workOrder.contactEmail}
                    </a>
                  </div>
                </div>
              )}
            </div>
          </div>

          {/* Client */}
          {workOrder.clientName && (
            <div className="px-6 sm:px-8 pb-8">
              <p className="text-xs text-gray-400">Client: {workOrder.clientName}</p>
            </div>
          )}
        </div>

        {/* Action Buttons */}
        <div className="space-y-3">
          <Button size="lg" onClick={handleDownloadPdf} loading={downloadingPdf} className="w-full" variant="secondary">
            <ArrowDownTrayIcon className="w-5 h-5 mr-2" />
            Download PDF
          </Button>
          {workOrder.quoteToken && (
            <Link to={`/quotes/submit/${workOrder.quoteToken}`} className="block">
              <Button size="lg" className="w-full">
                Submit Quote
              </Button>
            </Link>
          )}
        </div>

        {/* Footer */}
        <div className="mt-8 text-center">
          <div className="flex items-center justify-center gap-2 opacity-40">
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 48 28" className="w-6 h-4">
              <rect width="48" height="28" rx="14" fill="#E8511A" />
              <circle cx="34" cy="14" r="10" fill="white" />
            </svg>
            <span className="text-xs text-gray-400">Powered by On-Call</span>
          </div>
        </div>
      </div>
    </div>
  );
}
