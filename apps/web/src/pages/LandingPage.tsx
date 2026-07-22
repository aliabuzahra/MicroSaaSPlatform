import { Link } from 'react-router-dom';
import { Button } from '@saas/ui';

export function LandingPage() {
    return (
        <div className="min-h-screen bg-gradient-to-b from-blue-50 to-white">
            <header className="container mx-auto px-4 py-6 flex justify-between items-center">
                <h1 className="text-2xl font-bold text-blue-600">Multi Micro SaaS</h1>
                <nav className="flex gap-4">
                    <Link to="/login">
                        <Button>Sign In</Button>
                    </Link>
                    <Link to="/register">
                        <Button>Get Started</Button>
                    </Link>
                </nav>
            </header>

            <main className="container mx-auto px-4 py-20">
                <div className="text-center max-w-3xl mx-auto">
                    <h2 className="text-5xl font-bold text-gray-900 mb-6">
                        Build and Scale Your SaaS Business
                    </h2>
                    <p className="text-xl text-gray-600 mb-8">
                        A complete multi-tenant platform with authentication, billing, 
                        usage tracking, and more. Everything you need to launch your SaaS.
                    </p>
                    <div className="flex gap-4 justify-center">
                        <Link to="/register">
                            <Button>Start Free Trial</Button>
                        </Link>
                    </div>
                </div>

                <div className="mt-20 grid md:grid-cols-3 gap-8">
                    <div className="bg-white p-6 rounded-lg shadow-md">
                        <div className="w-12 h-12 bg-blue-100 rounded-lg flex items-center justify-center mb-4">
                            <svg className="w-6 h-6 text-blue-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z" />
                            </svg>
                        </div>
                        <h3 className="text-xl font-semibold mb-2">Secure Authentication</h3>
                        <p className="text-gray-600">JWT-based authentication with refresh tokens, password hashing, and multi-tenant isolation.</p>
                    </div>

                    <div className="bg-white p-6 rounded-lg shadow-md">
                        <div className="w-12 h-12 bg-green-100 rounded-lg flex items-center justify-center mb-4">
                            <svg className="w-6 h-6 text-green-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z" />
                            </svg>
                        </div>
                        <h3 className="text-xl font-semibold mb-2">Usage Tracking</h3>
                        <p className="text-gray-600">Track API calls, storage, and custom metrics. Set limits per plan and monitor in real-time.</p>
                    </div>

                    <div className="bg-white p-6 rounded-lg shadow-md">
                        <div className="w-12 h-12 bg-purple-100 rounded-lg flex items-center justify-center mb-4">
                            <svg className="w-6 h-6 text-purple-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M3 10h18M7 15h1m4 0h1m-7 4h12a3 3 0 003-3V8a3 3 0 00-3-3H6a3 3 0 00-3 3v8a3 3 0 003 3z" />
                            </svg>
                        </div>
                        <h3 className="text-xl font-semibold mb-2">Integrated Billing</h3>
                        <p className="text-gray-600">Paddle integration for subscriptions, automatic webhooks, and customer self-service.</p>
                    </div>
                </div>
            </main>

            <footer className="container mx-auto px-4 py-8 text-center text-gray-500">
                <p>&copy; 2024 Multi Micro SaaS. All rights reserved.</p>
            </footer>
        </div>
    );
}
