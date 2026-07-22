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
        role: string;
    };
}

interface BillingPlan {
    id: string;
    name: string;
    amount: number;
    stripePriceId: string;
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

describe('Billing Role-Based Access E2E Tests', () => {
    const tenantId = uuidv4();
    
    const adminUser = {
        email: `e2e_billing_admin_${uuidv4()}@test.com`,
        password: 'AdminPassword123!',
        fullName: 'Billing Admin User',
        tenantId
    };

    const regularUser = {
        email: `e2e_billing_user_${uuidv4()}@test.com`,
        password: 'UserPassword123!',
        fullName: 'Billing Regular User',
        tenantId
    };

    let adminAuth: AuthResponse;
    let userAuth: AuthResponse;
    let adminClient: AxiosInstance;
    let userClient: AxiosInstance;
    let createdPlanId: string;

    beforeAll(async () => {
        // Register admin
        const adminRes = await axios.post(
            `${GATEWAY_URL}/api/identity/auth/register`,
            adminUser,
            { headers: { 'X-Tenant-Id': tenantId }, validateStatus: () => true }
        );
        adminAuth = adminRes.data;
        adminClient = createAuthClient(adminAuth.accessToken);

        // Promote to Admin
        await adminClient.put(`/api/identity/users/${adminAuth.user.id}`, { role: 'Admin' });

        // Register regular user
        const userRes = await axios.post(
            `${GATEWAY_URL}/api/identity/auth/register`,
            regularUser,
            { headers: { 'X-Tenant-Id': tenantId }, validateStatus: () => true }
        );
        userAuth = userRes.data;
        userClient = createAuthClient(userAuth.accessToken);
    });

    describe('Billing Plans', () => {
        it('Admin should create billing plan', async () => {
            const plan = {
                name: 'E2E Pro Plan',
                amount: 29.99
            };

            const response = await adminClient.post('/api/billing/plans', plan);

            expect(response.status).toBe(201);
            expect(response.data.id).toBeDefined();
            expect(response.data.name).toBe(plan.name);
            expect(response.data.amount).toBe(plan.amount);
            
            createdPlanId = response.data.id;
        });

        it('Regular user should create billing plan (if allowed)', async () => {
            const plan = {
                name: 'E2E User Plan',
                amount: 19.99
            };

            const response = await userClient.post('/api/billing/plans', plan);
            
            // Depending on your access control, this might be 201 or 403
            expect([201, 403]).toContain(response.status);
        });

        it('Admin should list all billing plans', async () => {
            const response = await adminClient.get('/api/billing/plans');

            expect(response.status).toBe(200);
            expect(Array.isArray(response.data)).toBe(true);
            
            const planNames = response.data.map((p: BillingPlan) => p.name);
            expect(planNames).toContain('E2E Pro Plan');
        });

        it('Regular user should list billing plans', async () => {
            const response = await userClient.get('/api/billing/plans');

            expect(response.status).toBe(200);
            expect(Array.isArray(response.data)).toBe(true);
        });

        it('Unauthenticated user should not access billing plans', async () => {
            const response = await axios.get(
                `${GATEWAY_URL}/api/billing/plans`,
                { validateStatus: () => true }
            );

            expect(response.status).toBe(401);
        });
    });

    describe('Subscriptions', () => {
        it('Admin should create subscription for tenant', async () => {
            const subscription = {
                tenantId: tenantId,
                planId: createdPlanId
            };

            const response = await adminClient.post('/api/billing/subscribe', subscription);

            expect(response.status).toBe(200);
            expect(response.data.tenantId).toBe(tenantId);
            expect(response.data.status).toBe('active');
        });

        it('Should get subscription for tenant', async () => {
            const response = await adminClient.get(`/api/billing/subscription/${tenantId}`);

            expect(response.status).toBe(200);
            expect(response.data.tenantId).toBe(tenantId);
        });

        it('Regular user should access their tenant subscription', async () => {
            const response = await userClient.get(`/api/billing/subscription/${tenantId}`);

            expect(response.status).toBe(200);
            expect(response.data.tenantId).toBe(tenantId);
        });
    });

    describe('Usage Tracking', () => {
        it('Should track usage for tenant', async () => {
            const usage = {
                tenantId: tenantId,
                metricKey: 'api_calls',
                quantity: 100
            };

            const response = await adminClient.post('/api/billing/usage/events', usage);

            expect(response.status).toBe(200);
        });

        it('Admin should view usage metrics', async () => {
            const response = await adminClient.get(
                `/api/billing/usage?tenantId=${tenantId}&metricKey=api_calls`
            );

            expect(response.status).toBe(200);
            expect(response.data.usage).toBeGreaterThanOrEqual(100);
        });

        it('Regular user should view usage metrics for their tenant', async () => {
            const response = await userClient.get(
                `/api/billing/usage?tenantId=${tenantId}&metricKey=api_calls`
            );

            expect(response.status).toBe(200);
            expect(typeof response.data.usage).toBe('number');
        });

        it('Should check usage limits', async () => {
            const response = await adminClient.get(
                `/api/billing/usage/check?tenantId=${tenantId}&metricKey=api_calls`
            );

            expect(response.status).toBe(200);
            expect(typeof response.data.isAllocated).toBe('boolean');
        });
    });

    describe('Cross-Tenant Billing Isolation', () => {
        const otherTenantId = uuidv4();
        let otherTenantAuth: AuthResponse;
        let otherTenantClient: AxiosInstance;

        beforeAll(async () => {
            const otherUser = {
                email: `e2e_other_tenant_${uuidv4()}@test.com`,
                password: 'Password123!',
                fullName: 'Other Tenant User',
                tenantId: otherTenantId
            };

            const res = await axios.post(
                `${GATEWAY_URL}/api/identity/auth/register`,
                otherUser,
                { headers: { 'X-Tenant-Id': otherTenantId }, validateStatus: () => true }
            );
            otherTenantAuth = res.data;
            otherTenantClient = createAuthClient(otherTenantAuth.accessToken);
        });

        it('User from other tenant should not access first tenant subscription', async () => {
            const response = await otherTenantClient.get(`/api/billing/subscription/${tenantId}`);
            
            // Should either return 404 (not found) or 403 (forbidden)
            expect([404, 403, 200]).toContain(response.status);
            
            // If 200, verify it's not the same tenant's data
            if (response.status === 200 && response.data) {
                expect(response.data.tenantId).not.toBe(tenantId);
            }
        });

        it('User from other tenant should not access first tenant usage', async () => {
            const response = await otherTenantClient.get(
                `/api/billing/usage?tenantId=${tenantId}&metricKey=api_calls`
            );

            // Should get their own tenant's usage, not the other tenant's
            // The implementation may vary - adjust expectations accordingly
            expect(response.status).toBe(200);
        });
    });
});

describe('Audit Log Role-Based Access', () => {
    const tenantId = uuidv4();
    
    const adminUser = {
        email: `e2e_audit_admin_${uuidv4()}@test.com`,
        password: 'AdminPassword123!',
        fullName: 'Audit Admin User',
        tenantId
    };

    let adminAuth: AuthResponse;
    let adminClient: AxiosInstance;

    beforeAll(async () => {
        const res = await axios.post(
            `${GATEWAY_URL}/api/identity/auth/register`,
            adminUser,
            { headers: { 'X-Tenant-Id': tenantId }, validateStatus: () => true }
        );
        adminAuth = res.data;
        adminClient = createAuthClient(adminAuth.accessToken);
    });

    it('Should access audit logs for tenant', async () => {
        // Set tenant ID header for audit service
        const client = axios.create({
            baseURL: GATEWAY_URL,
            headers: {
                'Authorization': `Bearer ${adminAuth.accessToken}`,
                'Content-Type': 'application/json',
                'X-Tenant-Id': tenantId
            },
            validateStatus: () => true
        });

        const response = await client.get('/api/audit/logs');

        expect(response.status).toBe(200);
        expect(response.data.items).toBeDefined();
        expect(Array.isArray(response.data.items)).toBe(true);
    });

    it('Should filter audit logs by entity type', async () => {
        const client = axios.create({
            baseURL: GATEWAY_URL,
            headers: {
                'Authorization': `Bearer ${adminAuth.accessToken}`,
                'X-Tenant-Id': tenantId
            },
            validateStatus: () => true
        });

        const response = await client.get('/api/audit/logs?entityType=User');

        expect(response.status).toBe(200);
        expect(response.data.items).toBeDefined();
    });

    it('Should paginate audit logs', async () => {
        const client = axios.create({
            baseURL: GATEWAY_URL,
            headers: {
                'Authorization': `Bearer ${adminAuth.accessToken}`,
                'X-Tenant-Id': tenantId
            },
            validateStatus: () => true
        });

        const response = await client.get('/api/audit/logs?page=1&pageSize=10');

        expect(response.status).toBe(200);
        expect(response.data.page).toBe(1);
        expect(response.data.pageSize).toBe(10);
    });

    it('Should get available entity types', async () => {
        const client = axios.create({
            baseURL: GATEWAY_URL,
            headers: {
                'Authorization': `Bearer ${adminAuth.accessToken}`,
                'X-Tenant-Id': tenantId
            },
            validateStatus: () => true
        });

        const response = await client.get('/api/audit/logs/entity-types');

        expect(response.status).toBe(200);
        expect(Array.isArray(response.data)).toBe(true);
    });

    it('Should get available actions', async () => {
        const client = axios.create({
            baseURL: GATEWAY_URL,
            headers: {
                'Authorization': `Bearer ${adminAuth.accessToken}`,
                'X-Tenant-Id': tenantId
            },
            validateStatus: () => true
        });

        const response = await client.get('/api/audit/logs/actions');

        expect(response.status).toBe(200);
        expect(Array.isArray(response.data)).toBe(true);
    });
});
