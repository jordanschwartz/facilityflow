import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { Toaster } from 'react-hot-toast';
import { useEffect } from 'react';
import { useAuthStore } from './stores/authStore';
import { authApi } from './api/auth';
import AppShell from './components/layout/AppShell';
import LoginPage from './pages/auth/LoginPage';
import RegisterPage from './pages/auth/RegisterPage';
import DashboardPage from './pages/dashboard/DashboardPage';
import RequestListPage from './pages/requests/RequestListPage';
import RequestNewPage from './pages/requests/RequestNewPage';
import RequestDetailPage from './pages/requests/RequestDetailPage';
import VendorListPage from './pages/vendors/VendorListPage';
import VendorNewPage from './pages/vendors/VendorNewPage';
import VendorDetailPage from './pages/vendors/VendorDetailPage';
import ClientListPage from './pages/clients/ClientListPage';
import ClientNewPage from './pages/clients/ClientNewPage';
import ClientDetailPage from './pages/clients/ClientDetailPage';
import QuoteSubmitPage from './pages/public/QuoteSubmitPage';
import ProposalViewPage from './pages/public/ProposalViewPage';
import InvoiceListPage from './pages/invoices/InvoiceListPage';
import InvoiceDetailPage from './pages/invoices/InvoiceDetailPage';
import UserListPage from './pages/admin/UserListPage';
import UserNewPage from './pages/admin/UserNewPage';
import UserDetailPage from './pages/admin/UserDetailPage';
import ProfilePage from './pages/settings/ProfilePage';

const queryClient = new QueryClient({
  defaultOptions: { queries: { staleTime: 30000, retry: 1 } }
});

function ProtectedRoute({ children }: { children: React.ReactNode }) {
  const { token } = useAuthStore();
  return token ? <>{children}</> : <Navigate to="/login" replace />;
}

function AppInitializer() {
  const { token, user, setAuth } = useAuthStore();
  useEffect(() => {
    if (token && !user) {
      authApi.me().then(res => setAuth(token, res.data)).catch(() => {});
    }
  }, [token, user, setAuth]);
  return null;
}

export default function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <AppInitializer />
        <Toaster position="top-right" />
        <Routes>
          <Route path="/login" element={<LoginPage />} />
          <Route path="/register" element={<RegisterPage />} />
          <Route path="/quotes/submit/:token" element={<QuoteSubmitPage />} />
          <Route path="/proposals/view/:token" element={<ProposalViewPage />} />
          <Route path="/" element={<ProtectedRoute><AppShell /></ProtectedRoute>}>
            <Route index element={<Navigate to="/dashboard" replace />} />
            <Route path="dashboard" element={<DashboardPage />} />
            <Route path="work-orders" element={<RequestListPage />} />
            <Route path="work-orders/new" element={<RequestNewPage />} />
            <Route path="work-orders/:id" element={<RequestDetailPage />} />
            <Route path="invoices" element={<InvoiceListPage />} />
            <Route path="invoices/:id" element={<InvoiceDetailPage />} />
            <Route path="vendors" element={<VendorListPage />} />
            <Route path="vendors/new" element={<VendorNewPage />} />
            <Route path="vendors/:id" element={<VendorDetailPage />} />
            <Route path="clients" element={<ClientListPage />} />
            <Route path="clients/new" element={<ClientNewPage />} />
            <Route path="clients/:id" element={<ClientDetailPage />} />
            <Route path="admin/users" element={<UserListPage />} />
            <Route path="admin/users/new" element={<UserNewPage />} />
            <Route path="admin/users/:id" element={<UserDetailPage />} />
            <Route path="settings/profile" element={<ProfilePage />} />
            {/* Redirects from old routes */}
            <Route path="requests" element={<Navigate to="/work-orders" replace />} />
            <Route path="requests/new" element={<Navigate to="/work-orders/new" replace />} />
            <Route path="requests/:id" element={<Navigate to="/work-orders/:id" replace />} />
          </Route>
        </Routes>
      </BrowserRouter>
    </QueryClientProvider>
  );
}
