import React, { useEffect, useState } from 'react';

interface UsageData {
    usage: number;
    limit: number | null;
    isAllocated: boolean;
}

interface UsageComponentProps {
    tenantId: string;
    metricKey: string;
    label: string;
}

export const UsageComponent: React.FC<UsageComponentProps> = ({ tenantId, metricKey, label }) => {
    const [data, setData] = useState<UsageData | null>(null);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        fetch(`/api/billing/usage?tenantId=${tenantId}&metricKey=${metricKey}`)
            .then(res => res.json())
            .then(data => {
                setData(data);
                setLoading(false);
            })
            .catch(err => {
                console.error(err);
                setLoading(false);
            });
    }, [tenantId, metricKey]);

    if (loading) return <div className="animate-pulse h-16 bg-gray-100 rounded mb-4"></div>;
    if (!data) return null;

    const percentage = data.limit ? Math.min((data.usage / data.limit) * 100, 100) : 0;
    const isOverLimit = !data.isAllocated;

    return (
        <div className="mb-6 p-4 border rounded bg-white shadow-sm">
            <div className="flex justify-between mb-2">
                <span className="font-medium text-gray-700">{label}</span>
                <span className={`text-sm ${isOverLimit ? 'text-red-600 font-bold' : 'text-gray-500'}`}>
                    {data.usage} / {data.limit === null ? '∞' : data.limit}
                </span>
            </div>

            {data.limit !== null ? (
                <div className="w-full bg-gray-200 rounded-full h-2.5">
                    <div
                        className={`h-2.5 rounded-full ${isOverLimit ? 'bg-red-600' : 'bg-blue-600'}`}
                        style={{ width: `${percentage}%` }}
                    ></div>
                </div>
            ) : (
                <div className="text-xs text-green-600 font-semibold bg-green-50 px-2 py-1 rounded inline-block">
                    Unlimited
                </div>
            )}

            {isOverLimit && (
                <p className="text-xs text-red-600 mt-2">
                    ⚠️ Usage limit exceeded. Please upgrade your plan.
                </p>
            )}
        </div>
    );
};
