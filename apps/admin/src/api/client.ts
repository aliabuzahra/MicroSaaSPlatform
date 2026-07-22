export const API_BASE_URL = import.meta.env.VITE_API_URL || "http://localhost:5000/api";

function getAuthHeaders(): HeadersInit {
    const token = localStorage.getItem('accessToken');
    const headers: HeadersInit = {
        'Content-Type': 'application/json',
    };
    if (token) {
        headers['Authorization'] = `Bearer ${token}`;
    }
    return headers;
}

async function handleResponse(res: Response) {
    if (res.status === 401) {
        localStorage.removeItem('accessToken');
        localStorage.removeItem('refreshToken');
        localStorage.removeItem('user');
        window.location.href = '/login';
        throw new Error('Unauthorized');
    }
    if (!res.ok) {
        const error = await res.json().catch(() => ({ message: 'Request failed' }));
        throw new Error(error.message || 'Request failed');
    }
    return res.json();
}

export async function fetchTenants() {
    const res = await fetch(`${API_BASE_URL}/tenants`, {
        headers: getAuthHeaders(),
    });
    return handleResponse(res);
}

export async function createTenant(data: { name: string; slug: string; email: string }) {
    const res = await fetch(`${API_BASE_URL}/tenants`, {
        method: "POST",
        headers: getAuthHeaders(),
        body: JSON.stringify(data),
    });
    return handleResponse(res);
}

export async function updateTenant(id: string, data: { name?: string; slug?: string; email?: string }) {
    const res = await fetch(`${API_BASE_URL}/tenants/${id}`, {
        method: "PUT",
        headers: getAuthHeaders(),
        body: JSON.stringify(data),
    });
    return handleResponse(res);
}

export async function deleteTenant(id: string) {
    const res = await fetch(`${API_BASE_URL}/tenants/${id}`, {
        method: "DELETE",
        headers: getAuthHeaders(),
    });
    if (res.status === 204) return;
    return handleResponse(res);
}

export async function fetchPlans() {
    const res = await fetch(`${API_BASE_URL}/billing/plans`, {
        headers: getAuthHeaders(),
    });
    return handleResponse(res);
}

export async function createPlan(data: { name: string; amount: number }) {
    const res = await fetch(`${API_BASE_URL}/billing/plans`, {
        method: "POST",
        headers: getAuthHeaders(),
        body: JSON.stringify(data),
    });
    return handleResponse(res);
}

export async function fetchUsers() {
    const res = await fetch(`${API_BASE_URL}/identity/users`, {
        headers: getAuthHeaders(),
    });
    return handleResponse(res);
}

export async function updateUser(id: string, data: { fullName?: string; role?: string }) {
    const res = await fetch(`${API_BASE_URL}/identity/users/${id}`, {
        method: "PUT",
        headers: getAuthHeaders(),
        body: JSON.stringify(data),
    });
    return handleResponse(res);
}

export async function deactivateUser(id: string) {
    const res = await fetch(`${API_BASE_URL}/identity/users/${id}/deactivate`, {
        method: "POST",
        headers: getAuthHeaders(),
    });
    return handleResponse(res);
}

export async function activateUser(id: string) {
    const res = await fetch(`${API_BASE_URL}/identity/users/${id}/activate`, {
        method: "POST",
        headers: getAuthHeaders(),
    });
    return handleResponse(res);
}

export async function deleteUser(id: string) {
    const res = await fetch(`${API_BASE_URL}/identity/users/${id}`, {
        method: "DELETE",
        headers: getAuthHeaders(),
    });
    if (res.status === 204) return;
    return handleResponse(res);
}
