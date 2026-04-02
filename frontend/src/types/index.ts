export type UserRole = 'Operator' | 'Client' | 'Vendor';
export type Priority = 'Low' | 'Medium' | 'High' | 'Urgent';
export type ServiceRequestStatus = 'New' | 'Sourcing' | 'Quoting' | 'PendingApproval' | 'Approved' | 'Rejected' | 'Completed';
export type QuoteStatus = 'Requested' | 'Submitted' | 'Reviewed' | 'Selected' | 'Rejected';
export type ProposalStatus = 'Draft' | 'Sent' | 'Viewed' | 'Approved' | 'Rejected' | 'Revised';
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
export interface VendorSummary { id: string; companyName: string; trades: string[]; rating?: number; isDnu?: boolean; isActive?: boolean; }
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
  id: string;
  userId?: string;
  companyName: string;
  primaryContactName: string;
  email: string;
  phone?: string;
  primaryZip: string;
  serviceRadiusMiles: number;
  trades: string[];
  rating?: number;
  isActive: boolean;
  isDnu: boolean;
  dnuReason?: string;
  user?: AuthUser;
}

export interface VendorNote {
  id: string;
  vendorId: string;
  text: string;
  attachmentUrl?: string;
  attachmentFilename?: string;
  createdByName: string;
  createdAt: string;
}

export interface VendorPayment {
  id: string;
  vendorId: string;
  workOrderId?: string;
  amount: number;
  status: 'Pending' | 'Paid';
  paidAt?: string;
  notes?: string;
  createdAt: string;
}

export interface VendorSourcingResult {
  vendorId: string;
  companyName: string;
  primaryContactName: string;
  email: string;
  primaryZip: string;
  serviceRadiusMiles: number;
  trades: string[];
  isDnu: boolean;
  dnuReason?: string;
  completedJobCount: number;
  lastUsedDate?: string;
}

export interface Client {
  id: string; userId: string; companyName: string; phone: string; address: string; user: AuthUser;
}

export interface VendorInvite {
  id: string; serviceRequestId: string; vendorId: string;
  status: VendorInviteStatus; sentAt: string;
  vendor: VendorSummary; quote?: QuoteSummary;
}

export interface QuoteLineItem {
  id: string;
  description: string;
  quantity: number;
  unitPrice: number;
  total: number;
}

export interface Quote {
  id: string; serviceRequestId: string; vendorId: string;
  price: number; scopeOfWork: string; status: QuoteStatus;
  publicToken?: string; submittedAt?: string; vendor: VendorSummary; attachments: AttachmentDto[];
  proposedStartDate?: string;
  estimatedDurationValue?: number;
  estimatedDurationUnit?: string;
  notToExceedPrice?: number;
  assumptions?: string;
  exclusions?: string;
  vendorAvailability?: string;
  validUntil?: string;
  lineItems: QuoteLineItem[];
}

export interface ProposalQuoteSummary extends QuoteSummary {
  proposedStartDate?: string;
  estimatedDurationValue?: number;
  estimatedDurationUnit?: string;
  notToExceedPrice?: number;
  assumptions?: string;
  exclusions?: string;
}

export interface ProposalAttachment {
  id: string;
  proposalId: string;
  attachmentId: string;
  attachment: AttachmentDto;
}

export interface ProposalVersion {
  id: string;
  proposalId: string;
  versionNumber: number;
  price: number;
  vendorCost: number;
  marginPercentage: number;
  scopeOfWork: string;
  summary: string | null;
  notToExceedPrice: number | null;
  createdAt: string;
  changeNotes: string | null;
}

export interface Proposal {
  id: string;
  serviceRequestId: string;
  quoteId: string;
  price: number;
  vendorCost: number;
  marginPercentage: number;
  scopeOfWork: string;
  summary: string | null;
  summaryGeneratedByAi: boolean;
  notToExceedPrice: number | null;
  useNtePricing: boolean;
  proposedStartDate: string | null;
  estimatedDuration: string | null;
  termsAndConditions: string | null;
  internalNotes: string | null;
  status: ProposalStatus;
  publicToken?: string;
  version: number;
  sentAt?: string;
  clientResponse?: string;
  clientRespondedAt?: string;
  serviceRequest: ServiceRequestSummary;
  quote: ProposalQuoteSummary;
  attachments: ProposalAttachment[];
  versions: ProposalVersion[];
}

export interface ClientProposal {
  id: string;
  price: number;
  scopeOfWork: string;
  summary: string | null;
  notToExceedPrice: number | null;
  useNtePricing: boolean;
  proposedStartDate: string | null;
  estimatedDuration: string | null;
  termsAndConditions: string | null;
  status: ProposalStatus;
  sentAt?: string;
  clientResponse?: string;
  clientRespondedAt?: string;
  attachments: { id: string; fileName: string; filePath: string }[];
  serviceRequest: { title: string; location: string; category: string };
}

export interface CreateProposalRequest {
  quoteId: string;
  marginPercentage: number;
  scopeOfWork: string;
  summary?: string;
  notToExceedPrice?: number;
  useNtePricing?: boolean;
  proposedStartDate?: string;
  estimatedDuration?: string;
  termsAndConditions?: string;
  internalNotes?: string;
  attachmentIds?: string[];
}

export interface UpdateProposalRequest {
  marginPercentage?: number;
  scopeOfWork?: string;
  summary?: string;
  notToExceedPrice?: number;
  useNtePricing?: boolean;
  proposedStartDate?: string;
  estimatedDuration?: string;
  termsAndConditions?: string;
  internalNotes?: string;
  attachmentIds?: string[];
  changeNotes?: string;
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
