import React from 'react';

type CommonButtonProps = {
  className?: string;
  disabled?: boolean;
  title?: string;
};

export function IconButton({
  className = '',
  disabled,
  title,
  onClick,
  children,
}: React.PropsWithChildren<
  CommonButtonProps & {
    onClick?: React.MouseEventHandler<HTMLButtonElement>;
  }
>) {
  return (
    <button
      type="button"
      title={title}
      disabled={disabled}
      onClick={onClick}
      className={`h-10 w-10 glass-control-solid border border-white/[0.06] text-white/60 hover:text-white hover:bg-white/[0.06] hover:border-white/20 transition-all disabled:opacity-40 disabled:cursor-not-allowed disabled:hover:bg-transparent flex items-center justify-center ${className}`.trim()}
    >
      {children}
    </button>
  );
}
