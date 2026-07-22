import { describe, it, expect, beforeAll } from 'vitest';
import axios, { AxiosInstance } from 'axios';
import { v4 as uuidv4 } from 'uuid';

const GATEWAY_URL = process.env.GATEWAY_URL || 'http://localhost:5000';

interface AuthResponse {
    accessToken: string;
    refreshToken: string;
    user: {
        id: string;
        email: string;
        tenantId: string;
    };
}

interface Tenant {
    id: string;
    name: string;
    slug: string;
    email?: string;
    subscriptionPlan: string;
    isActive: boolean;
    createdAt: string;
}

function createAuthClient(token: string): AxiosInstance {
    return axios.create({
        baseURL: GATEWAY_URL,
        headers: {
            'Authorization': `Bearer ${token}`,
            'Content-Type': 'application/json'
        },
        validateStatus: () => true
    });
}

describe('Tenant CRUD E2E Tests', () => {
    const existingTenantId = uuidv4();
    
    const adminUser = {
        email: `e2e_tenant_admin_${uuidv4()}@test.com`,
        password: 'AdminPassword123!',
        fullName: 'Tenant Admin User',
        tenantId: existingTenantId
    };

    let authResponse: AuthResponse;
    let authClient: AxiosInstance;
    let createdTenantId: string;

    beforeAll(async () => {
        // Register admin user
        const res = await axios.post(
            `${GATEWAY_URL}/api/identity/auth/register`,
            adminUser,
            { headers: { 'X-Tenant-Id': existingTenantId }, validateStatus: () => true }
        );
        authResponse = res.data;
        authClient = createAuthClient(authResponse.accessToken);
    });

    describe('Create Tenant', () => {
        it('should create a new tenant', async () => {
            const newTenant = {
                name: 'E2E Test Tenant',
                slug: `e2e-tenant-${uuidv4().substring(0, 8)}`,
                email: 'admin@e2e-tenant.com'
            };

            const response = await authClient.post('/api/tenants', newTenant);

            expect(response.status).toBe(201);
            expect(response.data.id).toBeDefined();
            expect(response.data.name).toBe(newTenant.name);
            expect(response.data.slug).toBe(newTenant.slug);
            expect(response.data.subscriptionPlan).toBe('Free');
            expect(response.data.isActive).toBe(true);
            
            createdTenantId = response.data.id;
        });

        it('should reject duplicate slug', async () => {
            const duplicateTenant = {
                name: 'Duplicate Tenant',
                slug: `e2e-tenant-${uuidv4().substring(0, 8)}`,
                email: 'duplicate@test.com'
            };

            // Create first
            await authClient.post('/api/tenants', duplicateTenant);

            // Try to create duplicate
            const response = await authClient.post('/api/tenants', duplicateTenant);
            expect(response.status).toBe(409);
        });

        it('should reject empty name', async () => {
            const invalidTenant = {
                name: '',
                slug: 'valid-slug',
                email: 'test@test.com'
            };

            const response = await authClient.post('/api/tenants', invalidTenant);
            expect(response.status).toBe(400);
        });

        it('should reject empty slug', async () => {
            const invalidTenant = {
                name: 'Valid Name',
                slug: '',
                email: 'test@test.com'
            };

            const response = await authClient.post('/api/tenants', invalidTenant);
            expect(response.status).toBe(400);
        });
    });

    describe('Read Tenant', () => {
        it('should list all tenants', async () => {
            const response = await authClient.get('/api/tenants');

            expect(response.status).toBe(200);
            expect(Array.isArray(response.data)).toBe(true);
        });

        it('should filter active tenants only', async () => {
            const response = await authClient.get('/api/tenants?activeOnly=true');

            expect(response.status).toBe(200);
            expect(Array.isArray(response.data)).toBe(true);
            response.data.forEach((tenant: Tenant) => {
                expect(tenant.isActive).toBe(true);
            });
        });

        it('should get tenant by ID', async () => {
            const response = await authClient.get(`/api/tenants/${createdTenantId}`);

            expect(response.status).toBe(200);
            expect(response.data.id).toBe(createdTenantId);
        });

        it('should get tenant by slug', async () => {
            // First get the tenant to know its slug
            const tenantRes = await authClient.get(`/api/tenants/${createdTenantId}`);
            const slug = tenantRes.data.slug;

            const response = await authClient.get(`/api/tenants/by-slug/${slug}`);

            expect(response.status).toBe(200);
            expect(response.data.slug).toBe(slug);
        });

        it('should return 404 for non-existent tenant', async () => {
            const response = await authClient.get(`/api/tenants/${uuidv4()}`);
            expect(response.status).toBe(404);
        });
    });

    describe('Update Tenant', () => {
        it('should update tenant name', async () => {
            const response = await authClient.put(
                `/api/tenants/${createdTenantId}`,
                { name: 'Updated Tenant Name' }
            );

            expect(response.status).toBe(200);
            expect(response.data.name).toBe('Updated Tenant Name');
        });

        it('should update tenant email', async () => {
            const response = await authClient.put(
                `/api/tenants/${createdTenantId}`,
                { email: 'updated@e2e-tenant.com' }
            );

            expect(response.status).toBe(200);
            expect(response.data.email).toBe('updated@e2e-tenant.com');
        });

        it('should update tenant slug', async () => {
            const newSlug = `updated-slug-${uuidv4().substring(0, 8)}`;
            const response = await authClient.put(
                `/api/tenants/${createdTenantId}`,
                { slug: newSlug }
            );

            expect(response.status).toBe(200);
            expect(response.data.slug).toBe(newSlug);
        });

        it('should reject duplicate slug on update', async () => {
            // Create another tenant
            const anotherTenant = {
                name: 'Another Tenant',
                slug: `another-${uuidv4().substring(0, 8)}`
            };
            const createRes = await authClient.post('/api/tenants', anotherTenant);
            
            // Try to update to use the same slug
            const response = await authClient.put(
                `/api/tenants/${createdTenantId}`,
                { slug: createRes.data.slug }
            );

            expect(response.status).toBe(409);
        });
    });

    describe('Tenant Settings', () => {
        it('should get empty settings for new tenant', async () => {
            const response = await authClient.get(`/api/tenants/${createdTenantId}/settings`);

            expect(response.status).toBe(200);
            expect(typeof response.data).toBe('object');
        });

        it('should update tenant settings', async () => {
            const settings = {
                theme: 'dark',
                language: 'en',
                timezone: 'UTC'
            };

            const response = await authClient.put(
                `/api/tenants/${createdTenantId}/settings`,
                settings
            );

            expect(response.status).toBe(200);
            expect(response.data.theme).toBe('dark');
            expect(response.data.language).toBe('en');
            expect(response.data.timezone).toBe('UTC');
        });

        it('should persist settings', async () => {
            const response = await authClient.get(`/api/tenants/${createdTenantId}/settings`);

            expect(response.status).toBe(200);
            expect(response.data.theme).toBe('dark');
        });

        it('should add new settings while keeping existing', async () => {
            const response = await authClient.put(
                `/api/tenants/${createdTenantId}/settings`,
                { newSetting: 'value' }
            );

            expect(response.status).toBe(200);
            expect(response.data.theme).toBe('dark');
            expect(response.data.newSetting).toBe('value');
        });

        it('should delete a setting', async () => {
            const response = await authClient.delete(
                `/api/tenants/${createdTenantId}/settings/newSetting`
            );

            expect(response.status).toBe(204);

            // Verify deletion
            const getRes = await authClient.get(`/api/tenants/${createdTenantId}/settings`);
            expect(getRes.data.newSetting).toBeUndefined();
        });
    });

    describe('Deactivate/Activate Tenant', () => {
        it('should deactivate tenant', async () => {
            const response = await authClient.post(`/api/tenants/${createdTenantId}/deactivate`);
            expect(response.status).toBe(200);
        });

        it('should show tenant as inactive', async () => {
            const response = await authClient.get(`/api/tenants/${createdTenantId}`);
            expect(response.status).toBe(200);
            expect(response.data.isActive).toBe(false);
        });

        it('should not appear in active-only list', async () => {
            const response = await authClient.get('/api/tenants?activeOnly=true');
            const tenantIds = response.data.map((t: Tenant) => t.id);
            expect(tenantIds).not.toContain(createdTenantId);
        });

        it('should reactivate tenant', async () => {
            const response = await authClient.post(`/api/tenants/${createdTenantId}/activate`);
            expect(response.status).toBe(200);
        });

        it('should show tenant as active again', async () => {
            const response = await authClient.get(`/api/tenants/${createdTenantId}`);
            expect(response.status).toBe(200);
            expect(response.data.isActive).toBe(true);
        });
    });

    describe('Delete Tenant', () => {
        let tenantToDelete: string;

        beforeAll(async () => {
            // Create a tenant specifically for deletion
            const res = await authClient.post('/api/tenants', {
                name: 'Tenant To Delete',
                slug: `delete-me-${uuidv4().substring(0, 8)}`
            });
            tenantToDelete = res.data.id;

            // Add some settings
            await authClient.put(`/api/tenants/${tenantToDelete}/settings`, {
                setting1: 'value1'
            });
        });

        it('should delete tenant and its settings', async () => {
            const response = await authClient.delete(`/api/tenants/${tenantToDelete}`);
            expect(response.status).toBe(204);
        });

        it('should not find deleted tenant', async () => {
            const response = await authClient.get(`/api/tenants/${tenantToDelete}`);
            expect(response.status).toBe(404);
        });

        it('should not find settings for deleted tenant', async () => {
            const response = await authClient.get(`/api/tenants/${tenantToDelete}/settings`);
            expect(response.status).toBe(404);
        });
    });
});
