import { useState } from 'react';
import { Check } from 'lucide-react';
import { getPaddle } from '../lib/paddle';

const PLANS = [
    {
        id: 'pri_12345', // Replace with real Paddle Price ID
        name: 'Pro Plan',
        price: '$29 / month',
        features: ['Unlimited Projects', 'Advanced Analytics', 'Priority Support']
    }
];

export const PricingTable = ({ tenantId }: { tenantId: string }) => {
    const [loading, setLoading] = useState(false);

    const handleCheckout = async (priceId: string) => {
        setLoading(true);
        const paddle = await getPaddle();

        if (paddle) {
            paddle.Checkout.open({
                items: [{ priceId, quantity: 1 }],
                customData: {
                    tenantId: tenantId
                },
                settings: {
                    successUrl: window.location.href // Reload page on success
                }
            });
        }
        setLoading(false);
    };

    return (
        <div className="grid grid-cols-1 md:grid-cols-3 gap-6 mt-8">
            {PLANS.map((plan) => (
                <div key={plan.id} className="border rounded-lg p-6 shadow-sm hover:shadow-md transition-shadow">
                    <h3 className="text-xl font-bold mb-2">{plan.name}</h3>
                    <p className="text-2xl font-bold mb-4">{plan.price}</p>
                    <ul className="space-y-2 mb-6">
                        {plan.features.map((feature, i) => (
                            <li key={i} className="flex items-center text-sm text-gray-600">
                                <Check className="w-4 h-4 mr-2 text-green-500" />
                                {feature}
                            </li>
                        ))}
                    </ul>
                    <button
                        onClick={() => handleCheckout(plan.id)}
                        disabled={loading}
                        className="w-full bg-blue-600 text-white py-2 px-4 rounded hover:bg-blue-700 disabled:opacity-50"
                    >
                        {loading ? 'Loading...' : 'Upgrade Now'}
                    </button>
                </div>
            ))}
            <div className="col-span-1 md:col-span-3 text-center text-xs text-gray-500 mt-4">
                By subscribing, you agree to our <a href="/terms" target="_blank" className="underline">Terms and Conditions</a>.
            </div>
        </div>
    );
};
