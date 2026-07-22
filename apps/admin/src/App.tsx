import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { BrowserRouter, Routes, Route } from "react-router-dom";
import { AuthProvider } from "./context/AuthContext";
import { PrivateRoute } from "./components/PrivateRoute";
import { DashboardLayout } from "./layouts/DashboardLayout";
import { TenantsPage } from "./pages/TenantsPage";
import { PlansPage } from "./pages/PlansPage";
import { UsersPage } from "./pages/UsersPage";
import { SettingsPage } from "./pages/SettingsPage";
import { LoginPage } from "./pages/LoginPage";
import { BillingDashboard } from "./pages/billing/BillingDashboard";
import { TermsPage } from "./pages/TermsPage";

const queryClient = new QueryClient();

function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <AuthProvider>
        <BrowserRouter>
          <Routes>
            <Route path="/login" element={<LoginPage />} />
            <Route path="/terms" element={<TermsPage />} />
            <Route path="/" element={
              <PrivateRoute>
                <DashboardLayout />
              </PrivateRoute>
            }>
              <Route index element={<TenantsPage />} />
              <Route path="tenants" element={<TenantsPage />} />
              <Route path="plans" element={<PlansPage />} />
              <Route path="users" element={<UsersPage />} />
              <Route path="billing" element={<BillingDashboard />} />
              <Route path="settings" element={<SettingsPage />} />
            </Route>
          </Routes>
        </BrowserRouter>
      </AuthProvider>
    </QueryClientProvider>
  );
}

export default App;
