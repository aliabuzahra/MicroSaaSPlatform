import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
    stages: [
        { duration: '30s', target: 20 }, // Ramp up to 20 users
        { duration: '1m', target: 20 },  // Stay at 20 users
        { duration: '30s', target: 0 },  // Ramp down
    ],
    thresholds: {
        http_req_duration: ['p(95)<500'], // 95% of requests must be faster than 500ms
        http_req_failed: ['rate<0.01'],   // Error rate must be less than 1%
    },
};

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';

export default function () {
    // 1. Health Check
    const resHealth = http.get(`${BASE_URL}/health`);
    check(resHealth, { 'Health status is 200': (r) => r.status === 200 });

    // 2. Public API (simulate)
    // const resTenants = http.get(`${BASE_URL}/api/tenants`);
    // check(resTenants, { 'Tenants status is 200': (r) => r.status === 200 });

    sleep(1);
}
