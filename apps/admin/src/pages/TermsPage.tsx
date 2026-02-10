

export const TermsPage = () => {
    return (
        <div className="p-8 max-w-4xl mx-auto bg-white shadow rounded-lg mt-8">
            <h1 className="text-3xl font-bold mb-6">Terms and Conditions</h1>
            <p className="text-sm text-gray-500 mb-8">Last Updated: {new Date().toLocaleDateString()}</p>

            <div className="space-y-6 text-gray-700">
                <section>
                    <h2 className="text-xl font-semibold mb-2">1. Introduction</h2>
                    <p>
                        Welcome to Multi Micro SaaS ("Company", "we", "our", "us"). These Terms and Conditions ("Terms", "Terms and Conditions") govern your use of our website and our mobile application (together or individually "Service") operated by Multi Micro SaaS.
                    </p>
                </section>

                <section>
                    <h2 className="text-xl font-semibold mb-2">2. Subscriptions</h2>
                    <p>
                        Some parts of the Service are billed on a subscription basis ("Subscription(s)"). You will be billed in advance on a recurring and periodic basis ("Billing Cycle"). Billing cycles are set either on a monthly or annual basis, depending on the type of subscription plan you select when purchasing a Subscription.
                    </p>
                    <p className="mt-2">
                        At the end of each Billing Cycle, your Subscription will automatically renew under the exact same conditions unless you cancel it or Multi Micro SaaS cancels it. You may cancel your Subscription renewal either through your online account management page or by contacting Multi Micro SaaS customer support team.
                    </p>
                </section>

                <section>
                    <h2 className="text-xl font-semibold mb-2">3. Fee Changes</h2>
                    <p>
                        Multi Micro SaaS, in its sole discretion and at any time, may modify the Subscription fees for the Subscriptions. Any Subscription fee change will become effective at the end of the then-current Billing Cycle.
                    </p>
                </section>

                <section>
                    <h2 className="text-xl font-semibold mb-2">4. Refunds</h2>
                    <p>
                        Certain refund requests for Subscriptions may be considered by Multi Micro SaaS on a case-by-case basis and granted in sole discretion of Multi Micro SaaS.
                    </p>
                </section>

                <section>
                    <h2 className="text-xl font-semibold mb-2">5. Accounts</h2>
                    <p>
                        When you create an account with us, you must provide us information that is accurate, complete, and current at all times. Failure to do so constitutes a breach of the Terms, which may result in immediate termination of your account on our Service.
                    </p>
                </section>

                <section>
                    <h2 className="text-xl font-semibold mb-2">6. Contact Us</h2>
                    <p>
                        If you have any questions about these Terms, please contact us.
                    </p>
                </section>
            </div>
        </div>
    );
};
