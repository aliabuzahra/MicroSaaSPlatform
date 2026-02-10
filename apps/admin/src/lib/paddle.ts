import { initializePaddle, type Paddle } from '@paddle/paddle-js';

let paddle: Paddle | undefined;

export const getPaddle = async () => {
    if (paddle) return paddle;

    const clientToken = import.meta.env.VITE_PADDLE_CLIENT_TOKEN;
    if (!clientToken) {
        console.warn('Paddle Client Token not found in environment variables.');
        return undefined;
    }

    // Initialize Paddle in Sandbox mode by default for dev
    paddle = await initializePaddle({
        environment: 'sandbox',
        token: clientToken,
        eventCallback: (data) => {
            console.log('Paddle Event:', data);
        }
    });

    return paddle;
};
