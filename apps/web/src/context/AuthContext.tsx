import { createContext, useContext, useState, useEffect, ReactNode, useCallback } from 'react';

interface User {
    id: string;
    email: string;
    fullName: string;
    role: string;
    tenantId: string;
}

interface AuthState {
    user: User | null;
    accessToken: string | null;
    isAuthenticated: boolean;
    isLoading: boolean;
}

interface AuthContextType extends AuthState {
    login: (email: string, password: string) => Promise<void>;
    register: (email: string, password: string, fullName: string, tenantName: string) => Promise<void>;
    logout: () => void;
}

const AuthContext = createContext<AuthContextType | null>(null);

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000/api';

export function AuthProvider({ children }: { children: ReactNode }) {
    const [state, setState] = useState<AuthState>({
        user: null,
        accessToken: null,
        isAuthenticated: false,
        isLoading: true,
    });

    const setAuth = (accessToken: string, refreshToken: string, user: User) => {
        localStorage.setItem('accessToken', accessToken);
        localStorage.setItem('refreshToken', refreshToken);
        localStorage.setItem('user', JSON.stringify(user));
        setState({
            user,
            accessToken,
            isAuthenticated: true,
            isLoading: false,
        });
    };

    const clearAuth = () => {
        localStorage.removeItem('accessToken');
        localStorage.removeItem('refreshToken');
        localStorage.removeItem('user');
        setState({
            user: null,
            accessToken: null,
            isAuthenticated: false,
            isLoading: false,
        });
    };

    const login = async (email: string, password: string) => {
        const response = await fetch(`${API_BASE_URL}/identity/auth/login`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ email, password }),
        });

        if (!response.ok) {
            const error = await response.json().catch(() => ({ message: 'Login failed' }));
            throw new Error(error.message || 'Login failed');
        }

        const data = await response.json();
        setAuth(data.accessToken, data.refreshToken, data.user);
    };

    const register = async (email: string, password: string, fullName: string, tenantName: string) => {
        const tenantResponse = await fetch(`${API_BASE_URL}/tenants`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ 
                name: tenantName, 
                slug: tenantName.toLowerCase().replace(/[^a-z0-9]/g, '-'),
                email 
            }),
        });

        if (!tenantResponse.ok) {
            const error = await tenantResponse.json().catch(() => ({ message: 'Failed to create organization' }));
            throw new Error(error.message || 'Failed to create organization');
        }

        const tenant = await tenantResponse.json();

        const response = await fetch(`${API_BASE_URL}/identity/auth/register`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ 
                email, 
                password, 
                fullName,
                tenantId: tenant.id 
            }),
        });

        if (!response.ok) {
            const error = await response.json().catch(() => ({ message: 'Registration failed' }));
            throw new Error(error.message || 'Registration failed');
        }

        const data = await response.json();
        setAuth(data.accessToken, data.refreshToken, data.user);
    };

    const logout = () => {
        clearAuth();
    };

    useEffect(() => {
        const storedToken = localStorage.getItem('accessToken');
        const storedUser = localStorage.getItem('user');

        if (storedToken && storedUser) {
            try {
                const user = JSON.parse(storedUser);
                setState({
                    user,
                    accessToken: storedToken,
                    isAuthenticated: true,
                    isLoading: false,
                });
            } catch {
                clearAuth();
            }
        } else {
            setState(prev => ({ ...prev, isLoading: false }));
        }
    }, []);

    return (
        <AuthContext.Provider value={{ ...state, login, register, logout }}>
            {children}
        </AuthContext.Provider>
    );
}

export function useAuth() {
    const context = useContext(AuthContext);
    if (!context) {
        throw new Error('useAuth must be used within an AuthProvider');
    }
    return context;
}
