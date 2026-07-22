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

interface NotificationTemplate {
    id: string;
    key: string;
    name: string;
    subject: string;
    body?: string;
    isHtml: boolean;
    isActive: boolean;
}

function createAuthClient(token: string, tenantId?: string): AxiosInstance {
    const headers: Record<string, string> = {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json'
    };
    if (tenantId) {
        headers['X-Tenant-Id'] = tenantId;
    }
    return axios.create({
        baseURL: GATEWAY_URL,
        headers,
        validateStatus: () => true
    });
}

describe('Notifications Role-Based Access E2E Tests', () => {
    const tenantId = uuidv4();
    
    const adminUser = {
        email: `e2e_notif_admin_${uuidv4()}@test.com`,
        password: 'AdminPassword123!',
        fullName: 'Notifications Admin User',
        tenantId
    };

    const regularUser = {
        email: `e2e_notif_user_${uuidv4()}@test.com`,
        password: 'UserPassword123!',
        fullName: 'Notifications Regular User',
        tenantId
    };

    let adminAuth: AuthResponse;
    let userAuth: AuthResponse;
    let adminClient: AxiosInstance;
    let userClient: AxiosInstance;
    let createdTemplateId: string;

    beforeAll(async () => {
        // Register admin
        const adminRes = await axios.post(
            `${GATEWAY_URL}/api/identity/auth/register`,
            adminUser,
            { headers: { 'X-Tenant-Id': tenantId }, validateStatus: () => true }
        );
        adminAuth = adminRes.data;
        adminClient = createAuthClient(adminAuth.accessToken, tenantId);

        // Promote to Admin
        await adminClient.put(`/api/identity/users/${adminAuth.user.id}`, { role: 'Admin' });

        // Register regular user
        const userRes = await axios.post(
            `${GATEWAY_URL}/api/identity/auth/register`,
            regularUser,
            { headers: { 'X-Tenant-Id': tenantId }, validateStatus: () => true }
        );
        userAuth = userRes.data;
        userClient = createAuthClient(userAuth.accessToken, tenantId);
    });

    describe('Notification Templates', () => {
        it('Should list default notification templates', async () => {
            const response = await adminClient.get('/api/notifications/templates');

            expect(response.status).toBe(200);
            expect(Array.isArray(response.data)).toBe(true);
            
            // Should have default templates
            const templateKeys = response.data.map((t: NotificationTemplate) => t.key);
            expect(templateKeys).toContain('welcome_email');
        });

        it('Admin should create notification template', async () => {
            const template = {
                key: `e2e_template_${uuidv4().substring(0, 8)}`,
                name: 'E2E Test Template',
                subject: 'Test Subject {{name}}',
                body: '<h1>Hello {{name}}</h1><p>This is a test email.</p>',
                isHtml: true
            };

            const response = await adminClient.post('/api/notifications/templates', template);

            expect(response.status).toBe(201);
            expect(response.data.id).toBeDefined();
            expect(response.data.key).toBe(template.key);
            expect(response.data.name).toBe(template.name);
            
            createdTemplateId = response.data.id;
        });

        it('Should get template by ID', async () => {
            const response = await adminClient.get(`/api/notifications/templates/${createdTemplateId}`);

            expect(response.status).toBe(200);
            expect(response.data.id).toBe(createdTemplateId);
            expect(response.data.body).toBeDefined();
        });

        it('Admin should update notification template', async () => {
            const response = await adminClient.put(
                `/api/notifications/templates/${createdTemplateId}`,
                {
                    name: 'Updated Template Name',
                    subject: 'Updated Subject {{name}}'
                }
            );

            expect(response.status).toBe(200);
            expect(response.data.name).toBe('Updated Template Name');
        });

        it('Admin should deactivate template', async () => {
            const response = await adminClient.put(
                `/api/notifications/templates/${createdTemplateId}`,
                { isActive: false }
            );

            expect(response.status).toBe(200);
            expect(response.data.isActive).toBe(false);
        });

        it('Admin should reactivate template', async () => {
            const response = await adminClient.put(
                `/api/notifications/templates/${createdTemplateId}`,
                { isActive: true }
            );

            expect(response.status).toBe(200);
            expect(response.data.isActive).toBe(true);
        });

        it('Should reject duplicate template key', async () => {
            const existingTemplate = await adminClient.get(`/api/notifications/templates/${createdTemplateId}`);
            
            const response = await adminClient.post('/api/notifications/templates', {
                key: existingTemplate.data.key,
                name: 'Duplicate Template',
                subject: 'Subject',
                body: 'Body'
            });

            expect(response.status).toBe(409);
        });
    });

    describe('Send Notifications', () => {
        it('Admin should send notification using template', async () => {
            const templateRes = await adminClient.get(`/api/notifications/templates/${createdTemplateId}`);
            
            const response = await adminClient.post('/api/notifications/notifications/send', {
                recipient: 'test@example.com',
                templateKey: templateRes.data.key,
                placeholders: {
                    name: 'Test User'
                },
                tenantId: tenantId
            });

            expect(response.status).toBe(200);
            expect(response.data.message).toBe('Notification queued');
        });

        it('Admin should send direct notification', async () => {
            const response = await adminClient.post('/api/notifications/notifications/send', {
                recipient: 'direct@example.com',
                subject: 'Direct Test Email',
                body: '<p>This is a direct test email.</p>',
                isHtml: true,
                tenantId: tenantId
            });

            expect(response.status).toBe(200);
            expect(response.data.message).toBe('Notification queued');
        });

        it('Regular user should send notification (if allowed)', async () => {
            const response = await userClient.post('/api/notifications/notifications/send', {
                recipient: 'user-sent@example.com',
                subject: 'User Sent Email',
                body: 'Test from regular user',
                tenantId: tenantId
            });

            // Depending on access control
            expect([200, 403]).toContain(response.status);
        });
    });

    describe('Notification Logs', () => {
        it('Admin should view notification logs', async () => {
            const response = await adminClient.get('/api/notifications/notifications/logs');

            expect(response.status).toBe(200);
            expect(response.data.items).toBeDefined();
            expect(Array.isArray(response.data.items)).toBe(true);
        });

        it('Should filter notification logs by status', async () => {
            const response = await adminClient.get('/api/notifications/notifications/logs?status=Sent');

            expect(response.status).toBe(200);
            expect(response.data.items).toBeDefined();
        });

        it('Should paginate notification logs', async () => {
            const response = await adminClient.get('/api/notifications/notifications/logs?page=1&pageSize=5');

            expect(response.status).toBe(200);
            expect(response.data.page).toBe(1);
            expect(response.data.pageSize).toBe(5);
        });

        it('Regular user should view notification logs (for their tenant)', async () => {
            const response = await userClient.get('/api/notifications/notifications/logs');

            expect(response.status).toBe(200);
            expect(response.data.items).toBeDefined();
        });
    });

    describe('Template Deletion', () => {
        let templateToDelete: string;

        beforeAll(async () => {
            const res = await adminClient.post('/api/notifications/templates', {
                key: `delete_me_${uuidv4().substring(0, 8)}`,
                name: 'Template to Delete',
                subject: 'Delete Me',
                body: 'Delete this template'
            });
            templateToDelete = res.data.id;
        });

        it('Admin should delete template', async () => {
            const response = await adminClient.delete(`/api/notifications/templates/${templateToDelete}`);
            expect(response.status).toBe(204);
        });

        it('Should not find deleted template', async () => {
            const response = await adminClient.get(`/api/notifications/templates/${templateToDelete}`);
            expect(response.status).toBe(404);
        });
    });
});

describe('Cross-Tenant Notification Isolation', () => {
    const tenantA = uuidv4();
    const tenantB = uuidv4();

    const userA = {
        email: `e2e_notif_tenantA_${uuidv4()}@test.com`,
        password: 'Password123!',
        fullName: 'Tenant A Notif User',
        tenantId: tenantA
    };

    const userB = {
        email: `e2e_notif_tenantB_${uuidv4()}@test.com`,
        password: 'Password123!',
        fullName: 'Tenant B Notif User',
        tenantId: tenantB
    };

    let authA: AuthResponse;
    let authB: AuthResponse;
    let clientA: AxiosInstance;
    let clientB: AxiosInstance;

    beforeAll(async () => {
        const resA = await axios.post(
            `${GATEWAY_URL}/api/identity/auth/register`,
            userA,
            { headers: { 'X-Tenant-Id': tenantA }, validateStatus: () => true }
        );
        authA = resA.data;
        clientA = createAuthClient(authA.accessToken, tenantA);

        const resB = await axios.post(
            `${GATEWAY_URL}/api/identity/auth/register`,
            userB,
            { headers: { 'X-Tenant-Id': tenantB }, validateStatus: () => true }
        );
        authB = resB.data;
        clientB = createAuthClient(authB.accessToken, tenantB);
    });

    it('Tenant A should send notification to their tenant', async () => {
        const response = await clientA.post('/api/notifications/notifications/send', {
            recipient: 'tenantA-recipient@test.com',
            subject: 'Tenant A Notification',
            body: 'This is from Tenant A',
            tenantId: tenantA
        });

        expect(response.status).toBe(200);
    });

    it('Tenant B should send notification to their tenant', async () => {
        const response = await clientB.post('/api/notifications/notifications/send', {
            recipient: 'tenantB-recipient@test.com',
            subject: 'Tenant B Notification',
            body: 'This is from Tenant B',
            tenantId: tenantB
        });

        expect(response.status).toBe(200);
    });

    it('Tenant A should only see their notifications in logs', async () => {
        const response = await clientA.get('/api/notifications/notifications/logs');

        expect(response.status).toBe(200);
        
        // All notifications should be for Tenant A
        response.data.items.forEach((log: { tenantId: string | null }) => {
            if (log.tenantId) {
                expect(log.tenantId).toBe(tenantA);
            }
        });
    });

    it('Tenant B should only see their notifications in logs', async () => {
        const response = await clientB.get('/api/notifications/notifications/logs');

        expect(response.status).toBe(200);
        
        // All notifications should be for Tenant B
        response.data.items.forEach((log: { tenantId: string | null }) => {
            if (log.tenantId) {
                expect(log.tenantId).toBe(tenantB);
            }
        });
    });
});
