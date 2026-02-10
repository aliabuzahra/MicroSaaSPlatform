
export interface ButtonProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
    children: React.ReactNode;
}

export const Button = ({ children, ...props }: ButtonProps) => {
    return (
        <button style={{ padding: '10px 20px', background: 'blue', color: 'white' }} {...props}>
            {children}
        </button>
    );
};
