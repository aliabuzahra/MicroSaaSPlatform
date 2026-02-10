import { Button } from '@saas/ui';

export const LoginForm = () => {
    return (
        <div style={{ border: '1px solid #ccc', padding: '20px' }}>
            <h2>Login</h2>
            <input type="text" placeholder="Username" />
            <input type="password" placeholder="Password" />
            <Button>Login</Button>
        </div>
    );
};
