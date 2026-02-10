import { Link, Outlet } from "react-router-dom";

export function DashboardLayout() {
    return (
        <div className="flex min-h-screen w-full flex-col">
            <header className="sticky top-0 z-10 flex h-14 items-center gap-4 border-b bg-background px-6">
                <Link to="/" className="flex items-center gap-2 font-semibold text-lg">
                    MicroSaaS Admin
                </Link>
                <nav className="flex gap-4 ml-6">
                    <Link to="/tenants" className="text-sm font-medium hover:underline underline-offset-4">
                        Tenants
                    </Link>
                    <Link to="/plans" className="text-sm font-medium hover:underline underline-offset-4">
                        Plans
                    </Link>
                </nav>
                <div className="ml-auto">
                    {/* User Menu placeholder */}
                    <span className="text-sm text-muted-foreground">Admin User</span>
                </div>
            </header>
            <main className="flex flex-1 flex-col gap-4 p-4 md:gap-8 md:p-8">
                <Outlet />
            </main>
        </div>
    );
}
