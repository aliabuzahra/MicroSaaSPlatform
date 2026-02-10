import { Button } from '@saas/ui';

export const TenantList = () => {
    return (
        <div style={{ border: '1px solid #ccc', padding: '20px' }}>
            <h2>Tenants</h2>
            <ul>
                <li>Tenant A</li>
                <li>Tenant B</li>
            </ul>
            <Button>Add Tenant</Button>
        </div>
    );
};
