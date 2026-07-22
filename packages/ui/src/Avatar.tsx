import { cn } from './utils';

export interface AvatarProps {
    src?: string;
    alt?: string;
    fallback?: string;
    size?: 'sm' | 'md' | 'lg' | 'xl';
    className?: string;
}

export function Avatar({ src, alt, fallback, size = 'md', className }: AvatarProps) {
    const sizeStyles = {
        sm: 'w-6 h-6 text-xs',
        md: 'w-8 h-8 text-sm',
        lg: 'w-10 h-10 text-base',
        xl: 'w-12 h-12 text-lg',
    };

    const getInitials = (name?: string) => {
        if (!name) return '?';
        const parts = name.split(' ');
        if (parts.length >= 2) {
            return `${parts[0][0]}${parts[1][0]}`.toUpperCase();
        }
        return name.slice(0, 2).toUpperCase();
    };

    if (src) {
        return (
            <img
                src={src}
                alt={alt || 'Avatar'}
                className={cn(
                    'rounded-full object-cover',
                    sizeStyles[size],
                    className
                )}
            />
        );
    }

    return (
        <div
            className={cn(
                'rounded-full bg-blue-100 text-blue-600 flex items-center justify-center font-medium',
                sizeStyles[size],
                className
            )}
        >
            {getInitials(fallback || alt)}
        </div>
    );
}

export interface AvatarGroupProps {
    children: React.ReactNode;
    max?: number;
    className?: string;
}

export function AvatarGroup({ children, max = 4, className }: AvatarGroupProps) {
    const childArray = Array.isArray(children) ? children : [children];
    const visibleChildren = childArray.slice(0, max);
    const remainingCount = childArray.length - max;

    return (
        <div className={cn('flex -space-x-2', className)}>
            {visibleChildren.map((child, index) => (
                <div key={index} className="ring-2 ring-white rounded-full">
                    {child}
                </div>
            ))}
            {remainingCount > 0 && (
                <div className="ring-2 ring-white rounded-full w-8 h-8 bg-gray-100 text-gray-600 flex items-center justify-center text-xs font-medium">
                    +{remainingCount}
                </div>
            )}
        </div>
    );
}
