import { useRef, useEffect, type ReactNode, type CSSProperties } from 'react';
import gsap from 'gsap';

interface Props {
  children: ReactNode;
  className?: string;
  style?: CSSProperties;
  hover?: boolean;
  delay?: number;
}

/**
 * Glassmorphism card with optional hover-tilt effect and reveal animation.
 */
export function GlassCard({ children, className = '', style, hover = true, delay = 0 }: Props) {
  const ref = useRef<HTMLDivElement>(null);

  // Reveal on mount
  useEffect(() => {
    if (ref.current) {
      gsap.from(ref.current, {
        opacity: 0,
        y: 20,
        duration: 0.55,
        ease: 'power3.out',
        delay: 0.1 + delay,
      });
    }
  }, [delay]);

  // Hover tilt
  const handleMouseMove = (e: React.MouseEvent<HTMLDivElement>) => {
    if (!hover || !ref.current) return;
    const rect = ref.current.getBoundingClientRect();
    const x = (e.clientX - rect.left) / rect.width - 0.5;
    const y = (e.clientY - rect.top) / rect.height - 0.5;
    gsap.to(ref.current, {
      rotateY: x * 4,
      rotateX: -y * 4,
      duration: 0.3,
      ease: 'power2.out',
    });
  };

  const handleMouseLeave = () => {
    if (!ref.current) return;
    gsap.to(ref.current, {
      rotateY: 0,
      rotateX: 0,
      duration: 0.5,
      ease: 'power3.out',
    });
  };

  return (
    <div
      ref={ref}
      className={`glass rounded-2xl ${className}`}
      style={{ perspective: '800px', transformStyle: 'preserve-3d', ...style }}
      onMouseMove={handleMouseMove}
      onMouseLeave={handleMouseLeave}
    >
      {children}
    </div>
  );
}
