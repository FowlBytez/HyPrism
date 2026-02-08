import { useRef, useState, useEffect, type ReactNode } from 'react';
import { useLocation } from 'react-router-dom';
import gsap from 'gsap';

interface Props {
  children: ReactNode;
}

/**
 * Animated page transition wrapper.
 * Old page fades/slides out, new page fades/slides in with GSAP.
 */
export function PageTransition({ children }: Props) {
  const location = useLocation();
  const [displayLocation, setDisplayLocation] = useState(location);
  const [currentChildren, setCurrentChildren] = useState(children);
  const containerRef = useRef<HTMLDivElement>(null);
  const isAnimating = useRef(false);

  useEffect(() => {
    if (location.pathname === displayLocation.pathname) {
      setCurrentChildren(children);
      return;
    }
    if (isAnimating.current) return;
    isAnimating.current = true;

    const el = containerRef.current;
    if (!el) {
      setDisplayLocation(location);
      setCurrentChildren(children);
      isAnimating.current = false;
      return;
    }

    // Exit animation
    gsap.to(el, {
      opacity: 0,
      y: -16,
      scale: 0.985,
      duration: 0.22,
      ease: 'power2.in',
      onComplete: () => {
        setDisplayLocation(location);
        setCurrentChildren(children);

        // Entry animation
        gsap.fromTo(
          el,
          { opacity: 0, y: 24, scale: 0.985 },
          {
            opacity: 1,
            y: 0,
            scale: 1,
            duration: 0.45,
            ease: 'power3.out',
            onComplete: () => {
              isAnimating.current = false;
            },
          },
        );
      },
    });
  }, [location, displayLocation, children]);

  // Initial entry
  useEffect(() => {
    if (containerRef.current) {
      gsap.fromTo(
        containerRef.current,
        { opacity: 0, y: 30 },
        { opacity: 1, y: 0, duration: 0.6, ease: 'power3.out', delay: 0.15 },
      );
    }
  }, []);

  return (
    <div ref={containerRef} className="page-transition">
      {currentChildren}
    </div>
  );
}
