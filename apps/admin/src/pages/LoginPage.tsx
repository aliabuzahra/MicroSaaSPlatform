import { useState } from "react";
import { useMutation } from "@tanstack/react-query";
import { Button, Input, Card, CardHeader, CardTitle, CardContent } from "@saas/ui";

const API_BASE_URL = "http://localhost:5000/api";

async function loginUser(data: { email: string; password: string }) {
    const res = await fetch(`${API_BASE_URL}/identity/auth/login`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(data),
    });
    if (!res.ok) throw new Error("Login failed");
    return res.json();
}

export function LoginPage() {
    const [email, setEmail] = useState("");
    const [password, setPassword] = useState("");
    const [error, setError] = useState("");

    const mutation = useMutation({
        mutationFn: loginUser,
        onSuccess: (data) => {
            // Store token in localStorage (POC)
            localStorage.setItem("token", data.token);
            window.location.href = "/";
        },
        onError: () => {
            setError("Invalid credentials");
        }
    });

    const handleLogin = (e: React.FormEvent) => {
        e.preventDefault();
        setError("");
        mutation.mutate({ email, password });
    };

    return (
        <div className="flex min-h-screen items-center justify-center bg-gray-50">
            <Card className="w-full max-w-md">
                <CardHeader>
                    <CardTitle className="text-center">Admin Login</CardTitle>
                </CardHeader>
                <CardContent>
                    <form onSubmit={handleLogin} className="space-y-4">
                        <div className="space-y-2">
                            <label htmlFor="email">Email</label>
                            <Input
                                id="email"
                                type="email"
                                value={email}
                                onChange={(e: React.ChangeEvent<HTMLInputElement>) => setEmail(e.target.value)}
                                required
                            />
                        </div>
                        <div className="space-y-2">
                            <label htmlFor="password">Password</label>
                            <Input
                                id="password"
                                type="password"
                                value={password}
                                onChange={(e: React.ChangeEvent<HTMLInputElement>) => setPassword(e.target.value)}
                                required
                            />
                        </div>
                        {error && <p className="text-red-500 text-sm">{error}</p>}
                        <Button type="submit" className="w-full" disabled={mutation.isPending}>
                            {mutation.isPending ? "Signing In..." : "Sign In"}
                        </Button>
                    </form>
                </CardContent>
            </Card>
        </div>
    );
}
