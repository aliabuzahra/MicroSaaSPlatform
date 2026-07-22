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
    refreshToken: string | null;
    isAuthenticated: boolean;
    isLoading: boolean;
}

interface AuthContextType extends AuthState {
    login: (email: string, password: string) => Promise<void>;
    logout: () => void;
    refreshAuth: () => Promise<boolean>;
}

const AuthContext = createContext<AuthContextType | null>(null);

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000/api';

export function AuthProvider({ children }: { children: ReactNode }) {
    const [state, setState] = useState<AuthState>({
        user: null,
        accessToken: null,
        refreshToken: null,
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
            refreshToken,
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
            refreshToken: null,
            isAuthenticated: false,
            isLoading: false,
        });
    };

    const refreshAuth = useCallback(async (): Promise<boolean> => {
        const storedRefreshToken = localStorage.getItem('refreshToken');
        if (!storedRefreshToken) {
            clearAuth();
            return false;
        }

        try {
            const response = await fetch(`${API_BASE_URL}/identity/auth/refresh`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ refreshToken: storedRefreshToken }),
            });

            if (!response.ok) {
                clearAuth();
                return false;
            }

            const data = await response.json();
            setAuth(data.accessToken, data.refreshToken, data.user);
            return true;
        } catch {
            clearAuth();
            return false;
        }
    }, []);

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

    const logout = async () => {
        const refreshToken = localStorage.getItem('refreshToken');
        if (refreshToken) {
            try {
                await fetch(`${API_BASE_URL}/identity/auth/logout`, {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ refreshToken }),
                });
            } catch {
                // Ignore logout errors
            }
        }
        clearAuth();
    };

    useEffect(() => {
        const initAuth = async () => {
            const storedToken = localStorage.getItem('accessToken');
            const storedUser = localStorage.getItem('user');

            if (storedToken && storedUser) {
                try {
                    const user = JSON.parse(storedUser);
                    setState({
                        user,
                        accessToken: storedToken,
                        refreshToken: localStorage.getItem('refreshToken'),
                        isAuthenticated: true,
                        isLoading: false,
                    });
                } catch {
                    await refreshAuth();
                }
            } else {
                setState(prev => ({ ...prev, isLoading: false }));
            }
        };

        initAuth();
    }, [refreshAuth]);

    return (
        <AuthContext.Provider value={{ ...state, login, logout, refreshAuth }}>
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
