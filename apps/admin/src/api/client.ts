export const API_BASE_URL = "http://localhost:5000/api";

export async function fetchTenants() {
    const res = await fetch(`${API_BASE_URL}/tenants`);
    if (!res.ok) throw new Error("Failed to fetch tenants");
    return res.json();
}

export async function createTenant(data: { name: string; slug: string; email: string }) {
    const res = await fetch(`${API_BASE_URL}/tenants`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(data),
    });
    if (!res.ok) throw new Error("Failed to create tenant");
    return res.json();
}

export async function fetchPlans() {
    const res = await fetch(`${API_BASE_URL}/billing/plans`);
    if (!res.ok) throw new Error("Failed to fetch plans");
    return res.json();
}

export async function createPlan(data: { name: string; amount: number }) {
    const res = await fetch(`${API_BASE_URL}/billing/plans`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(data),
    });
    if (!res.ok) throw new Error("Failed to create plan");
    return res.json();
}
