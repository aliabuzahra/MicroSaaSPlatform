import { describe, it, expect } from 'vitest';
import axios from 'axios';
import { v4 as uuidv4 } from 'uuid';

const GATEWAY_URL = process.env.GATEWAY_URL || 'http://localhost:5050';

describe('Tenant Isolation E2E Tests', () => {
    const tenantA = uuidv4();
    const tenantB = uuidv4();

    const userA = {
        email: `e2e_userA_${uuidv4()}@tenant1.com`,
        password: 'Password123!',
        fullName: 'E2E User A',
        tenantId: tenantA
    };

    const userB = {
        email: `e2e_userB_${uuidv4()}@tenant2.com`,
        password: 'Password123!',
        fullName: 'E2E User B',
        tenantId: tenantB
    };

    it('should register User A in Tenant A', async () => {
        const response = await axios.post(`${GATEWAY_URL}/api/identity/auth/register`, userA, {
            headers: { 'X-Tenant-Id': tenantA },
            validateStatus: () => true
        });
        expect(response.status).toBe(200);
        expect(response.data.email).toBe(userA.email);
    });

    it('should register User B in Tenant B', async () => {
        const response = await axios.post(`${GATEWAY_URL}/api/identity/auth/register`, userB, {
            headers: { 'X-Tenant-Id': tenantB },
            validateStatus: () => true
        });
        expect(response.status).toBe(200);
        expect(response.data.email).toBe(userB.email);
    });

    it('should FAIL to login User A when using Tenant B context', async () => {
        const response = await axios.post(`${GATEWAY_URL}/api/identity/auth/login`, {
            email: userA.email,
            password: userA.password
        }, {
            headers: { 'X-Tenant-Id': tenantB }, // Wrong Tenant Context
            validateStatus: () => true
        });

        // Expecting 401 Unauthorized (User not found in Tenant B isolate)
        expect(response.status).toBe(401);
    });

    it('should SUCCEED to login User A when using Tenant A context', async () => {
        const response = await axios.post(`${GATEWAY_URL}/api/identity/auth/login`, {
            email: userA.email,
            password: userA.password
        }, {
            headers: { 'X-Tenant-Id': tenantA }, // Correct Tenant Context
            validateStatus: () => true
        });

        expect(response.status).toBe(200);
        expect(response.data.token).toBeDefined();
    });
});
