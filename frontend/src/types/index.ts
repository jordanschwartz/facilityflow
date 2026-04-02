export type UserRole = 'Operator' | 'Client' | 'Vendor';
export type Priority = 'Low' | 'Medium' | 'High' | 'Urgent';
export type ServiceRequestStatus = 'New' | 'Sourcing' | 'Quoting' | 'PendingApproval' | 'Approved' | 'Rejected' | 'Completed';
export type QuoteStatus = 'Requested' | 'Submitted' | 'Reviewed' | 'Selected' | 'Rejected';
export type ProposalStatus = 'Draft' | 'Sent' | 'Approved' | 'Rejected';
export type WorkOrderStatus = 'Assigned' | 'InProgress' | 'Completed' | 'Closed';
export type VendorInviteStatus = 'Invited' | 'Viewed' | 'Quoted' | 'Declined';

export interface AuthUser {
  id: string;
  email: string;
  name: string;
  role: UserRole;
  createdAt: string;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface ClientSummary { id: string; companyName: string; phone?: string; }
export interface VendorSummary { id: string; companyName: string; trades: string[]; rating?: number; }
export interface AttachmentDto { id: string; url: string; filename: string; mimeType: string; }
export interface QuoteSummary { id: string; status: QuoteStatus; price?: number; submittedAt?: string; }

export interface ServiceRequestSummary {
  id: string; title: string; priority: Priority; status: ServiceRequestStatus;
  clientId: string; createdAt: string; updatedAt: string;
  client: ClientSummary; quoteCount: number; hasProposal: boolean; hasWorkOrder: boolean;
}

export interface ServiceRequest extends ServiceRequestSummary {
  description: string; location: string; category: string;
  createdById: string; createdBy: AuthUser; attachments: AttachmentDto[];
}

export interface Vendor {
  id: string; userId: string; companyName: string; phone: string;
  trades: string[]; zipCodes: string[]; rating?: number; user: AuthUser;
}

export interface Client {
  id: string; userId: string; companyName: string; phone: string; address: string; user: AuthUser;
}

export interface VendorInvite {
  id: string; serviceRequestId: string; vendorId: string;
  status: VendorInviteStatus; sentAt: string;
  vendor: VendorSummary; quote?: QuoteSummary;
}

export interface Quote {
  id: string; serviceRequestId: string; vendorId: string;
  price: number; scopeOfWork: string; status: QuoteStatus;
  publicToken?: string; submittedAt?: string; vendor: VendorSummary; attachments: AttachmentDto[];
}

export interface Proposal {
  id: string; serviceRequestId: string; quoteId: string;
  price: number; scopeOfWork: string; status: ProposalStatus;
  publicToken?: string; sentAt?: string; clientResponse?: string; clientRespondedAt?: string;
  serviceRequest: ServiceRequestSummary; quote: QuoteSummary;
}

export interface WorkOrder {
  id: string; serviceRequestId: string; proposalId: string; vendorId: string;
  status: WorkOrderStatus; vendorNotes?: string; completedAt?: string;
  serviceRequest: ServiceRequestSummary; vendor: VendorSummary; attachments: AttachmentDto[];
}

export interface Comment {
  id: string; text: string; authorId: string;
  serviceRequestId?: string; quoteId?: string; workOrderId?: string;
  createdAt: string; author: AuthUser;
}

export interface Notification {
  id: string; type: string; message: string; read: boolean; link?: string; createdAt: string;
}

export interface PipelineColumn { count: number; items: ServiceRequestSummary[]; }
export interface DashboardStats {
  totalOpenRequests: number; pendingQuotes: number; awaitingApproval: number; completedThisMonth: number;
}
export interface PipelineResponse {
  columns: Record<string, PipelineColumn>;
  stats: DashboardStats;
}
