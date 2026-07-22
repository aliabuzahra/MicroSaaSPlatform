import { describe, it, expect, beforeAll } from 'vitest';
import axios, { AxiosInstance } from 'axios';
import { v4 as uuidv4 } from 'uuid';

const GATEWAY_URL = process.env.GATEWAY_URL || 'http://localhost:5000';

interface AuthResponse {
    accessToken: string;
    refreshToken: string;
    expiresIn: number;
    user: {
        id: string;
        email: string;
        fullName: string;
        role: string;
        tenantId: string;
    };
}

interface User {
    id: string;
    email: string;
    fullName: string;
    role: string;
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

describe('User Roles E2E Tests', () => {
    const tenantId = uuidv4();
    
    const adminUser = {
        email: `e2e_admin_${uuidv4()}@test.com`,
        password: 'AdminPassword123!',
        fullName: 'E2E Admin User',
        tenantId
    };

    const regularUser = {
        email: `e2e_user_${uuidv4()}@test.com`,
        password: 'UserPassword123!',
        fullName: 'E2E Regular User',
        tenantId
    };

    const secondUser = {
        email: `e2e_user2_${uuidv4()}@test.com`,
        password: 'User2Password123!',
        fullName: 'E2E Second User',
        tenantId
    };

    let adminAuth: AuthResponse;
    let userAuth: AuthResponse;
    let secondUserAuth: AuthResponse;
    let adminClient: AxiosInstance;
    let userClient: AxiosInstance;

    describe('User Registration', () => {
        it('should register an admin user successfully', async () => {
            const response = await axios.post(
                `${GATEWAY_URL}/api/identity/auth/register`,
                adminUser,
                { 
                    headers: { 'X-Tenant-Id': tenantId },
                    validateStatus: () => true 
                }
            );

            expect(response.status).toBe(200);
            expect(response.data.accessToken).toBeDefined();
            expect(response.data.refreshToken).toBeDefined();
            expect(response.data.user.email).toBe(adminUser.email);
            expect(response.data.user.role).toBe('User'); // Default role
            
            adminAuth = response.data;
            adminClient = createAuthClient(adminAuth.accessToken);
        });

        it('should register a regular user successfully', async () => {
            const response = await axios.post(
                `${GATEWAY_URL}/api/identity/auth/register`,
                regularUser,
                { 
                    headers: { 'X-Tenant-Id': tenantId },
                    validateStatus: () => true 
                }
            );

            expect(response.status).toBe(200);
            expect(response.data.accessToken).toBeDefined();
            expect(response.data.user.email).toBe(regularUser.email);
            expect(response.data.user.role).toBe('User');
            
            userAuth = response.data;
            userClient = createAuthClient(userAuth.accessToken);
        });

        it('should register a second user for role change tests', async () => {
            const response = await axios.post(
                `${GATEWAY_URL}/api/identity/auth/register`,
                secondUser,
                { 
                    headers: { 'X-Tenant-Id': tenantId },
                    validateStatus: () => true 
                }
            );

            expect(response.status).toBe(200);
            secondUserAuth = response.data;
        });

        it('should reject duplicate email registration in same tenant', async () => {
            const response = await axios.post(
                `${GATEWAY_URL}/api/identity/auth/register`,
                adminUser,
                { 
                    headers: { 'X-Tenant-Id': tenantId },
                    validateStatus: () => true 
                }
            );

            expect(response.status).toBe(409);
        });
    });

    describe('User Authentication', () => {
        it('should login with valid credentials and return JWT', async () => {
            const response = await axios.post(
                `${GATEWAY_URL}/api/identity/auth/login`,
                {
                    email: adminUser.email,
                    password: adminUser.password
                },
                { 
                    headers: { 'X-Tenant-Id': tenantId },
                    validateStatus: () => true 
                }
            );

            expect(response.status).toBe(200);
            expect(response.data.accessToken).toBeDefined();
            expect(response.data.refreshToken).toBeDefined();
            expect(response.data.expiresIn).toBeGreaterThan(0);
            expect(response.data.user.id).toBeDefined();
        });

        it('should reject login with invalid password', async () => {
            const response = await axios.post(
                `${GATEWAY_URL}/api/identity/auth/login`,
                {
                    email: adminUser.email,
                    password: 'WrongPassword123!'
                },
                { 
                    headers: { 'X-Tenant-Id': tenantId },
                    validateStatus: () => true 
                }
            );

            expect(response.status).toBe(401);
        });

        it('should reject login with non-existent email', async () => {
            const response = await axios.post(
                `${GATEWAY_URL}/api/identity/auth/login`,
                {
                    email: 'nonexistent@test.com',
                    password: 'Password123!'
                },
                { 
                    headers: { 'X-Tenant-Id': tenantId },
                    validateStatus: () => true 
                }
            );

            expect(response.status).toBe(401);
        });
    });

    describe('Token Refresh', () => {
        it('should refresh token successfully', async () => {
            const response = await axios.post(
                `${GATEWAY_URL}/api/identity/auth/refresh`,
                { refreshToken: adminAuth.refreshToken },
                { validateStatus: () => true }
            );

            expect(response.status).toBe(200);
            expect(response.data.accessToken).toBeDefined();
            expect(response.data.refreshToken).toBeDefined();
            expect(response.data.refreshToken).not.toBe(adminAuth.refreshToken); // Token rotation
            
            // Update admin auth with new tokens
            adminAuth = response.data;
            adminClient = createAuthClient(adminAuth.accessToken);
        });

        it('should reject invalid refresh token', async () => {
            const response = await axios.post(
                `${GATEWAY_URL}/api/identity/auth/refresh`,
                { refreshToken: 'invalid-refresh-token' },
                { validateStatus: () => true }
            );

            expect(response.status).toBe(401);
        });
    });

    describe('User Profile Access', () => {
        it('should get current user profile with valid token', async () => {
            const response = await adminClient.get('/api/identity/auth/me');

            expect(response.status).toBe(200);
            expect(response.data.email).toBe(adminUser.email);
            expect(response.data.fullName).toBe(adminUser.fullName);
        });

        it('should reject profile access without token', async () => {
            const response = await axios.get(
                `${GATEWAY_URL}/api/identity/auth/me`,
                { validateStatus: () => true }
            );

            expect(response.status).toBe(401);
        });

        it('should reject profile access with invalid token', async () => {
            const invalidClient = createAuthClient('invalid-token');
            const response = await invalidClient.get('/api/identity/auth/me');

            expect(response.status).toBe(401);
        });
    });

    describe('User Management - List Users', () => {
        it('should list all users in tenant with valid token', async () => {
            const response = await adminClient.get('/api/identity/users');

            expect(response.status).toBe(200);
            expect(Array.isArray(response.data)).toBe(true);
            expect(response.data.length).toBeGreaterThanOrEqual(3);
            
            const emails = response.data.map((u: User) => u.email);
            expect(emails).toContain(adminUser.email);
            expect(emails).toContain(regularUser.email);
            expect(emails).toContain(secondUser.email);
        });

        it('should reject user list without authentication', async () => {
            const response = await axios.get(
                `${GATEWAY_URL}/api/identity/users`,
                { validateStatus: () => true }
            );

            expect(response.status).toBe(401);
        });
    });

    describe('User Management - Update User Role', () => {
        it('should update user role to Admin', async () => {
            const response = await adminClient.put(
                `/api/identity/users/${adminAuth.user.id}`,
                { role: 'Admin' }
            );

            expect(response.status).toBe(200);
            expect(response.data.role).toBe('Admin');
        });

        it('should verify role change persists', async () => {
            const response = await adminClient.get(
                `/api/identity/users/${adminAuth.user.id}`
            );

            expect(response.status).toBe(200);
            expect(response.data.role).toBe('Admin');
        });

        it('should update another user\'s role', async () => {
            const response = await adminClient.put(
                `/api/identity/users/${secondUserAuth.user.id}`,
                { role: 'Admin' }
            );

            expect(response.status).toBe(200);
            expect(response.data.role).toBe('Admin');
        });

        it('should downgrade user role back to User', async () => {
            const response = await adminClient.put(
                `/api/identity/users/${secondUserAuth.user.id}`,
                { role: 'User' }
            );

            expect(response.status).toBe(200);
            expect(response.data.role).toBe('User');
        });
    });

    describe('User Management - Update Profile', () => {
        it('should update user full name', async () => {
            const newName = 'Updated Admin Name';
            const response = await adminClient.put(
                `/api/identity/users/${adminAuth.user.id}`,
                { fullName: newName }
            );

            expect(response.status).toBe(200);
            expect(response.data.fullName).toBe(newName);
        });

        it('should return 404 for non-existent user', async () => {
            const response = await adminClient.put(
                `/api/identity/users/${uuidv4()}`,
                { fullName: 'Test' }
            );

            expect(response.status).toBe(404);
        });
    });

    describe('User Management - Deactivate/Activate User', () => {
        it('should deactivate a user', async () => {
            const response = await adminClient.post(
                `/api/identity/users/${secondUserAuth.user.id}/deactivate`
            );

            expect(response.status).toBe(200);
        });

        it('should show user as inactive', async () => {
            const response = await adminClient.get(
                `/api/identity/users/${secondUserAuth.user.id}`
            );

            expect(response.status).toBe(200);
            expect(response.data.isActive).toBe(false);
        });

        it('should reject login for deactivated user', async () => {
            const response = await axios.post(
                `${GATEWAY_URL}/api/identity/auth/login`,
                {
                    email: secondUser.email,
                    password: secondUser.password
                },
                { 
                    headers: { 'X-Tenant-Id': tenantId },
                    validateStatus: () => true 
                }
            );

            expect(response.status).toBe(403);
        });

        it('should reactivate a user', async () => {
            const response = await adminClient.post(
                `/api/identity/users/${secondUserAuth.user.id}/activate`
            );

            expect(response.status).toBe(200);
        });

        it('should allow login after reactivation', async () => {
            const response = await axios.post(
                `${GATEWAY_URL}/api/identity/auth/login`,
                {
                    email: secondUser.email,
                    password: secondUser.password
                },
                { 
                    headers: { 'X-Tenant-Id': tenantId },
                    validateStatus: () => true 
                }
            );

            expect(response.status).toBe(200);
            expect(response.data.accessToken).toBeDefined();
        });
    });

    describe('User Management - Change Password', () => {
        it('should allow user to change their own password', async () => {
            const newPassword = 'NewUserPassword456!';
            const response = await userClient.post(
                `/api/identity/users/${userAuth.user.id}/change-password`,
                {
                    currentPassword: regularUser.password,
                    newPassword: newPassword
                }
            );

            expect(response.status).toBe(200);
            regularUser.password = newPassword;
        });

        it('should login with new password', async () => {
            const response = await axios.post(
                `${GATEWAY_URL}/api/identity/auth/login`,
                {
                    email: regularUser.email,
                    password: regularUser.password
                },
                { 
                    headers: { 'X-Tenant-Id': tenantId },
                    validateStatus: () => true 
                }
            );

            expect(response.status).toBe(200);
            userAuth = response.data;
            userClient = createAuthClient(userAuth.accessToken);
        });

        it('should reject password change with wrong current password', async () => {
            const response = await userClient.post(
                `/api/identity/users/${userAuth.user.id}/change-password`,
                {
                    currentPassword: 'WrongPassword123!',
                    newPassword: 'AnotherPassword789!'
                }
            );

            expect(response.status).toBe(400);
        });

        it('should reject password change for different user', async () => {
            const response = await userClient.post(
                `/api/identity/users/${adminAuth.user.id}/change-password`,
                {
                    currentPassword: 'SomePassword',
                    newPassword: 'AnotherPassword789!'
                }
            );

            expect(response.status).toBe(403);
        });
    });

    describe('Logout', () => {
        it('should logout and invalidate refresh token', async () => {
            const currentRefreshToken = userAuth.refreshToken;
            
            const logoutResponse = await userClient.post(
                '/api/identity/auth/logout',
                { refreshToken: currentRefreshToken }
            );

            expect(logoutResponse.status).toBe(200);

            // Try to use the old refresh token
            const refreshResponse = await axios.post(
                `${GATEWAY_URL}/api/identity/auth/refresh`,
                { refreshToken: currentRefreshToken },
                { validateStatus: () => true }
            );

            expect(refreshResponse.status).toBe(401);
        });
    });

    describe('User Deletion', () => {
        it('should delete a user', async () => {
            const response = await adminClient.delete(
                `/api/identity/users/${secondUserAuth.user.id}`
            );

            expect(response.status).toBe(204);
        });

        it('should not find deleted user', async () => {
            const response = await adminClient.get(
                `/api/identity/users/${secondUserAuth.user.id}`
            );

            expect(response.status).toBe(404);
        });

        it('should reject login for deleted user', async () => {
            const response = await axios.post(
                `${GATEWAY_URL}/api/identity/auth/login`,
                {
                    email: secondUser.email,
                    password: secondUser.password
                },
                { 
                    headers: { 'X-Tenant-Id': tenantId },
                    validateStatus: () => true 
                }
            );

            expect(response.status).toBe(401);
        });
    });
});

describe('Admin vs User Role Access Control', () => {
    const tenantId = uuidv4();
    
    const adminUser = {
        email: `e2e_roletest_admin_${uuidv4()}@test.com`,
        password: 'AdminPassword123!',
        fullName: 'Role Test Admin',
        tenantId
    };

    const regularUser = {
        email: `e2e_roletest_user_${uuidv4()}@test.com`,
        password: 'UserPassword123!',
        fullName: 'Role Test User',
        tenantId
    };

    let adminAuth: AuthResponse;
    let userAuth: AuthResponse;
    let adminClient: AxiosInstance;
    let userClient: AxiosInstance;

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

    describe('Resource Access by Role', () => {
        it('Admin should access user list', async () => {
            const response = await adminClient.get('/api/identity/users');
            expect(response.status).toBe(200);
            expect(Array.isArray(response.data)).toBe(true);
        });

        it('Regular user should also access user list (within tenant)', async () => {
            const response = await userClient.get('/api/identity/users');
            expect(response.status).toBe(200);
            expect(Array.isArray(response.data)).toBe(true);
        });

        it('Admin can update other users', async () => {
            const response = await adminClient.put(
                `/api/identity/users/${userAuth.user.id}`,
                { fullName: 'Updated by Admin' }
            );
            expect(response.status).toBe(200);
            expect(response.data.fullName).toBe('Updated by Admin');
        });

        it('Regular user can update themselves', async () => {
            const response = await userClient.put(
                `/api/identity/users/${userAuth.user.id}`,
                { fullName: 'Self Updated' }
            );
            expect(response.status).toBe(200);
            expect(response.data.fullName).toBe('Self Updated');
        });

        it('Admin can deactivate users', async () => {
            const response = await adminClient.post(
                `/api/identity/users/${userAuth.user.id}/deactivate`
            );
            expect(response.status).toBe(200);
        });

        it('Admin can reactivate users', async () => {
            const response = await adminClient.post(
                `/api/identity/users/${userAuth.user.id}/activate`
            );
            expect(response.status).toBe(200);
        });
    });
});

describe('Multi-Tenant Role Isolation', () => {
    const tenantA = uuidv4();
    const tenantB = uuidv4();
    
    const userTenantA = {
        email: `e2e_tenantA_${uuidv4()}@test.com`,
        password: 'Password123!',
        fullName: 'Tenant A User',
        tenantId: tenantA
    };

    const userTenantB = {
        email: `e2e_tenantB_${uuidv4()}@test.com`,
        password: 'Password123!',
        fullName: 'Tenant B User',
        tenantId: tenantB
    };

    let authA: AuthResponse;
    let authB: AuthResponse;
    let clientA: AxiosInstance;
    let clientB: AxiosInstance;

    beforeAll(async () => {
        const resA = await axios.post(
            `${GATEWAY_URL}/api/identity/auth/register`,
            userTenantA,
            { headers: { 'X-Tenant-Id': tenantA }, validateStatus: () => true }
        );
        authA = resA.data;
        clientA = createAuthClient(authA.accessToken);

        const resB = await axios.post(
            `${GATEWAY_URL}/api/identity/auth/register`,
            userTenantB,
            { headers: { 'X-Tenant-Id': tenantB }, validateStatus: () => true }
        );
        authB = resB.data;
        clientB = createAuthClient(authB.accessToken);
    });

    it('Tenant A user should only see Tenant A users', async () => {
        const response = await clientA.get('/api/identity/users');
        expect(response.status).toBe(200);
        
        const emails = response.data.map((u: User) => u.email);
        expect(emails).toContain(userTenantA.email);
        expect(emails).not.toContain(userTenantB.email);
    });

    it('Tenant B user should only see Tenant B users', async () => {
        const response = await clientB.get('/api/identity/users');
        expect(response.status).toBe(200);
        
        const emails = response.data.map((u: User) => u.email);
        expect(emails).toContain(userTenantB.email);
        expect(emails).not.toContain(userTenantA.email);
    });

    it('Tenant A user cannot access Tenant B user details', async () => {
        const response = await clientA.get(`/api/identity/users/${authB.user.id}`);
        expect(response.status).toBe(404);
    });

    it('Tenant B user cannot access Tenant A user details', async () => {
        const response = await clientB.get(`/api/identity/users/${authA.user.id}`);
        expect(response.status).toBe(404);
    });

    it('Same email can be used in different tenants', async () => {
        const sharedEmail = `shared_${uuidv4()}@test.com`;
        
        const resA = await axios.post(
            `${GATEWAY_URL}/api/identity/auth/register`,
            { ...userTenantA, email: sharedEmail },
            { headers: { 'X-Tenant-Id': tenantA }, validateStatus: () => true }
        );
        expect(resA.status).toBe(200);

        const resB = await axios.post(
            `${GATEWAY_URL}/api/identity/auth/register`,
            { ...userTenantB, email: sharedEmail },
            { headers: { 'X-Tenant-Id': tenantB }, validateStatus: () => true }
        );
        expect(resB.status).toBe(200);

        // Both should be able to login with their respective tenants
        const loginA = await axios.post(
            `${GATEWAY_URL}/api/identity/auth/login`,
            { email: sharedEmail, password: 'Password123!' },
            { headers: { 'X-Tenant-Id': tenantA }, validateStatus: () => true }
        );
        expect(loginA.status).toBe(200);
        expect(loginA.data.user.tenantId).toBe(tenantA);

        const loginB = await axios.post(
            `${GATEWAY_URL}/api/identity/auth/login`,
            { email: sharedEmail, password: 'Password123!' },
            { headers: { 'X-Tenant-Id': tenantB }, validateStatus: () => true }
        );
        expect(loginB.status).toBe(200);
        expect(loginB.data.user.tenantId).toBe(tenantB);
    });
});
