import { Link, Outlet, useLocation } from "react-router-dom";
import { useState } from "react";
import { useAuth } from "../context/AuthContext";

export function DashboardLayout() {
    const { user, logout } = useAuth();
    const location = useLocation();
    const [showUserMenu, setShowUserMenu] = useState(false);

    const navLinks = [
        { path: "/tenants", label: "Tenants" },
        { path: "/plans", label: "Plans" },
        { path: "/billing", label: "Billing" },
        { path: "/users", label: "Users" },
    ];

    const isActive = (path: string) => location.pathname === path || location.pathname.startsWith(path + '/');

    return (
        <div className="flex min-h-screen w-full flex-col">
            <header className="sticky top-0 z-10 flex h-14 items-center gap-4 border-b bg-white px-6 shadow-sm">
                <Link to="/" className="flex items-center gap-2 font-semibold text-lg text-blue-600">
                    MicroSaaS Admin
                </Link>
                <nav className="flex gap-6 ml-8">
                    {navLinks.map((link) => (
                        <Link
                            key={link.path}
                            to={link.path}
                            className={`text-sm font-medium transition-colors ${
                                isActive(link.path)
                                    ? 'text-blue-600 border-b-2 border-blue-600 pb-[17px]'
                                    : 'text-gray-600 hover:text-gray-900'
                            }`}
                        >
                            {link.label}
                        </Link>
                    ))}
                </nav>
                <div className="ml-auto relative">
                    <button
                        onClick={() => setShowUserMenu(!showUserMenu)}
                        className="flex items-center gap-2 text-sm text-gray-700 hover:text-gray-900"
                    >
                        <div className="w-8 h-8 rounded-full bg-blue-100 flex items-center justify-center text-blue-600 font-medium">
                            {user?.fullName?.charAt(0).toUpperCase() || 'U'}
                        </div>
                        <span className="hidden md:inline">{user?.fullName}</span>
                        <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" />
                        </svg>
                    </button>
                    
                    {showUserMenu && (
                        <div className="absolute right-0 mt-2 w-48 bg-white rounded-md shadow-lg py-1 border">
                            <div className="px-4 py-2 border-b">
                                <p className="text-sm font-medium text-gray-900">{user?.fullName}</p>
                                <p className="text-xs text-gray-500">{user?.email}</p>
                            </div>
                            <Link
                                to="/settings"
                                className="block px-4 py-2 text-sm text-gray-700 hover:bg-gray-100"
                                onClick={() => setShowUserMenu(false)}
                            >
                                Settings
                            </Link>
                            <button
                                onClick={() => {
                                    setShowUserMenu(false);
                                    logout();
                                }}
                                className="block w-full text-left px-4 py-2 text-sm text-red-600 hover:bg-gray-100"
                            >
                                Sign Out
                            </button>
                        </div>
                    )}
                </div>
            </header>
            <main className="flex flex-1 flex-col gap-4 p-4 md:gap-8 md:p-8 bg-gray-50">
                <Outlet />
            </main>
        </div>
    );
}
