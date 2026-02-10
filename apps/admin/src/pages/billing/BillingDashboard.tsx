import { useEffect, useState } from 'react';
import { PricingTable } from '../../components/PricingTable';
import { UsageComponent } from './UsageComponent';

interface Subscription {
    id: string;
    status: string;
    currentPeriodEnd: string;
    updateUrl?: string;
    cancelUrl?: string;
    planId: string;
}

export const BillingDashboard = () => {
    // TODO: Get TenantId from Auth Context
    const tenantId = "55555555-5555-5555-5555-555555555555";
    const [subscription, setSubscription] = useState<Subscription | null>(null);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        fetch(`/api/billing/subscription/${tenantId}`)
            .then(res => {
                if (res.ok) return res.json();
                return null; // 404 means no sub
            })
            .then(data => {
                setSubscription(data);
                setLoading(false);
            })
            .catch(err => {
                console.error(err);
                setLoading(false);
            });
    }, [tenantId]);

    if (loading) return <div className="p-8">Loading billing info...</div>;

    return (
        <div className="p-8 max-w-4xl mx-auto">
            <h1 className="text-3xl font-bold mb-6">Billing & Subscription</h1>

            {subscription && subscription.status === 'active' ? (
                <div className="bg-white p-6 rounded-lg shadow border">
                    <div className="flex justify-between items-start">
                        <div>
                            <h2 className="text-xl font-semibold text-green-700 mb-2">Current Plan: Active</h2>
                            <p className="text-gray-600">Next billing date: {new Date(subscription.currentPeriodEnd).toLocaleDateString()}</p>
                        </div>
                        <div className="space-x-4">
                            {subscription.updateUrl && (
                                <a href={subscription.updateUrl} target="_blank" rel="noreferrer" className="text-blue-600 hover:underline">
                                    Update Payment Method
                                </a>
                            )}
                            {subscription.cancelUrl && (
                                <a href={subscription.cancelUrl} target="_blank" rel="noreferrer" className="text-red-600 hover:underline">
                                    Cancel Subscription
                                </a>
                            )}
                        </div>
                    </div>
                </div>
            ) : (
                <div>
                    <div className="bg-yellow-50 p-4 rounded mb-6 border border-yellow-200">
                        <h2 className="font-semibold text-yellow-800">You are currently on the Free Plan</h2>
                        <p className="text-sm text-yellow-700">Upgrade to unlock more features.</p>
                    </div>
                </div>
            )}

            <div className="mt-8">
                <h2 className="text-2xl font-bold mb-4">Resource Usage</h2>
                <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                    <UsageComponent tenantId={tenantId} metricKey="test_metric" label="Test Metric (API Calls)" />
                    <UsageComponent tenantId={tenantId} metricKey="metric_B" label="Metric B (Storage)" />
                </div>
            </div>

            {(!subscription || subscription.status !== 'active') && (
                <PricingTable tenantId={tenantId} />
            )}
        </div>
    );
};
