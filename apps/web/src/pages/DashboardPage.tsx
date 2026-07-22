import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { Card, CardHeader, CardTitle, CardContent, Button } from '@saas/ui';
import { useAuth } from '../context/AuthContext';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000/api';

interface Subscription {
    id: string;
    status: string;
    planId: string;
    currentPeriodEnd: string;
}

interface UsageData {
    usage: number;
    limit: number | null;
    isAllocated: boolean;
}

export function DashboardPage() {
    const { user, logout } = useAuth();
    const [subscription, setSubscription] = useState<Subscription | null>(null);
    const [usage, setUsage] = useState<UsageData | null>(null);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        const fetchData = async () => {
            const token = localStorage.getItem('accessToken');
            const headers = { Authorization: `Bearer ${token}` };

            try {
                const [subRes, usageRes] = await Promise.all([
                    fetch(`${API_BASE_URL}/billing/subscription/${user?.tenantId}`, { headers }),
                    fetch(`${API_BASE_URL}/billing/usage?tenantId=${user?.tenantId}&metricKey=api_calls`, { headers })
                ]);

                if (subRes.ok) {
                    setSubscription(await subRes.json());
                }
                if (usageRes.ok) {
                    setUsage(await usageRes.json());
                }
            } catch (err) {
                console.error('Failed to fetch dashboard data', err);
            } finally {
                setLoading(false);
            }
        };

        if (user?.tenantId) {
            fetchData();
        }
    }, [user?.tenantId]);

    if (loading) {
        return (
            <div className="min-h-screen flex items-center justify-center">
                <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600"></div>
            </div>
        );
    }

    return (
        <div className="min-h-screen bg-gray-50">
            <header className="bg-white shadow-sm">
                <div className="container mx-auto px-4 py-4 flex justify-between items-center">
                    <h1 className="text-xl font-bold text-blue-600">Multi Micro SaaS</h1>
                    <div className="flex items-center gap-4">
                        <span className="text-sm text-gray-600">Welcome, {user?.fullName}</span>
                        <Button onClick={logout}>Sign Out</Button>
                    </div>
                </div>
            </header>

            <main className="container mx-auto px-4 py-8">
                <h2 className="text-2xl font-bold mb-6">Dashboard</h2>

                <div className="grid md:grid-cols-2 lg:grid-cols-3 gap-6">
                    <Card>
                        <CardHeader>
                            <CardTitle>Account</CardTitle>
                        </CardHeader>
                        <CardContent>
                            <div className="space-y-2">
                                <p><span className="font-medium">Name:</span> {user?.fullName}</p>
                                <p><span className="font-medium">Email:</span> {user?.email}</p>
                                <p><span className="font-medium">Role:</span> {user?.role}</p>
                            </div>
                        </CardContent>
                    </Card>

                    <Card>
                        <CardHeader>
                            <CardTitle>Subscription</CardTitle>
                        </CardHeader>
                        <CardContent>
                            {subscription ? (
                                <div className="space-y-2">
                                    <p>
                                        <span className="font-medium">Status:</span>{' '}
                                        <span className={`px-2 py-1 rounded text-xs ${
                                            subscription.status === 'active' 
                                                ? 'bg-green-100 text-green-800' 
                                                : 'bg-yellow-100 text-yellow-800'
                                        }`}>
                                            {subscription.status}
                                        </span>
                                    </p>
                                    <p><span className="font-medium">Next billing:</span> {new Date(subscription.currentPeriodEnd).toLocaleDateString()}</p>
                                </div>
                            ) : (
                                <div>
                                    <p className="text-gray-600 mb-4">You're on the free plan.</p>
                                    <Link to="/billing">
                                        <Button>Upgrade Now</Button>
                                    </Link>
                                </div>
                            )}
                        </CardContent>
                    </Card>

                    <Card>
                        <CardHeader>
                            <CardTitle>API Usage</CardTitle>
                        </CardHeader>
                        <CardContent>
                            {usage ? (
                                <div>
                                    <div className="flex justify-between mb-2">
                                        <span className="text-sm text-gray-600">
                                            {usage.usage} / {usage.limit === null ? '∞' : usage.limit}
                                        </span>
                                    </div>
                                    {usage.limit !== null && (
                                        <div className="w-full bg-gray-200 rounded-full h-2">
                                            <div
                                                className={`h-2 rounded-full ${
                                                    usage.usage / usage.limit > 0.9 ? 'bg-red-600' : 'bg-blue-600'
                                                }`}
                                                style={{ width: `${Math.min((usage.usage / usage.limit) * 100, 100)}%` }}
                                            ></div>
                                        </div>
                                    )}
                                </div>
                            ) : (
                                <p className="text-gray-600">No usage data available</p>
                            )}
                        </CardContent>
                    </Card>
                </div>

                <div className="mt-8">
                    <Card>
                        <CardHeader>
                            <CardTitle>Quick Actions</CardTitle>
                        </CardHeader>
                        <CardContent>
                            <div className="flex gap-4">
                                <Link to="/billing">
                                    <Button>Manage Billing</Button>
                                </Link>
                                <Link to="/settings">
                                    <Button>Settings</Button>
                                </Link>
                            </div>
                        </CardContent>
                    </Card>
                </div>
            </main>
        </div>
    );
}
