import React from 'react';

export type ButtonVariant = 'default' | 'primary' | 'danger';
export type ButtonSize = 'sm' | 'md';

type CommonButtonProps = {
  className?: string;
  disabled?: boolean;
  title?: string;
};

export function Button({
  variant = 'default',
  size = 'md',
  className = '',
  disabled,
  title,
  onClick,
  children,
  style,
  type = 'button',
}: React.PropsWithChildren<
  CommonButtonProps & {
    variant?: ButtonVariant;
    size?: ButtonSize;
    onClick?: React.MouseEventHandler<HTMLButtonElement>;
    style?: React.CSSProperties;
    type?: 'button' | 'submit' | 'reset';
  }
>) {
  const base =
    'inline-flex items-center justify-center gap-2 font-medium transition-all disabled:opacity-40 disabled:cursor-not-allowed disabled:hover:bg-transparent';
  const padding = size === 'sm' ? 'h-10 px-3 text-xs' : 'h-10 px-4 text-sm';

  // Default surface matches the “mirror/Test Speed” control style.
  const variantClass =
    variant === 'primary'
      ? 'shadow-sm'
      : variant === 'danger'
        ? 'bg-red-500/10 text-red-200 hover:bg-red-500/15 border border-red-500/20'
        : 'glass-control-solid text-white/70 hover:text-white hover:bg-white/[0.06] border border-white/[0.06] hover:border-white/20';

  return (
    <button
      type={type}
      title={title}
      disabled={disabled}
      onClick={onClick}
      style={style}
      className={`${base} ${padding} ${variantClass} ${className}`.trim()}
    >
      {children}
    </button>
  );
}
