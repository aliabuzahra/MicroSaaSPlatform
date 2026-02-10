import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { BrowserRouter, Routes, Route } from "react-router-dom";
import { DashboardLayout } from "./layouts/DashboardLayout";
import { TenantsPage } from "./pages/TenantsPage";
import { PlansPage } from "./pages/PlansPage";
import { LoginPage } from "./pages/LoginPage";
import { BillingDashboard } from "./pages/billing/BillingDashboard";
import { TermsPage } from "./pages/TermsPage";

const queryClient = new QueryClient();

function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <Routes>
          <Route path="/login" element={<LoginPage />} />
          <Route path="/" element={<DashboardLayout />}>
            <Route index element={<TenantsPage />} /> {/* Default to tenants for now */}
            <Route path="tenants" element={<TenantsPage />} />
            <Route path="plans" element={<PlansPage />} />
            <Route path="billing" element={<BillingDashboard />} />
            <Route path="terms" element={<TermsPage />} />
          </Route>
        </Routes>
      </BrowserRouter>
    </QueryClientProvider>
  );
}

export default App;
