export const statusColors: Record<string, string> = {
  // ServiceRequest
  New: 'bg-gray-100 text-gray-700',
  Sourcing: 'bg-blue-100 text-blue-700',
  Quoting: 'bg-yellow-100 text-yellow-700',
  PendingApproval: 'bg-purple-100 text-purple-700',
  Approved: 'bg-green-100 text-green-700',
  Rejected: 'bg-red-100 text-red-700',
  Completed: 'bg-emerald-100 text-emerald-700',
  // Quote
  Requested: 'bg-gray-100 text-gray-700',
  Submitted: 'bg-blue-100 text-blue-700',
  Reviewed: 'bg-yellow-100 text-yellow-700',
  Selected: 'bg-green-100 text-green-700',
  // Proposal
  Draft: 'bg-gray-100 text-gray-700',
  Sent: 'bg-blue-100 text-blue-700',
  // WorkOrder
  Assigned: 'bg-blue-100 text-blue-700',
  InProgress: 'bg-yellow-100 text-yellow-700',
  Closed: 'bg-gray-100 text-gray-700',
};

export const priorityColors: Record<string, string> = {
  Low: 'bg-gray-100 text-gray-600',
  Medium: 'bg-blue-100 text-blue-700',
  High: 'bg-orange-100 text-orange-700',
  Urgent: 'bg-red-100 text-red-700',
};
