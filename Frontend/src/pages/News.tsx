import { useState, useEffect, useRef } from 'react';
import { Newspaper, Clock, ExternalLink, RefreshCw, TrendingUp, Star } from 'lucide-react';
import gsap from 'gsap';
import { ipc } from '../lib/ipc';
import type { NewsItem } from '../lib/ipc';
import { GlassCard } from '../components/GlassCard';

const CATEGORIES = ['All', 'Update', 'Community', 'Dev Blog', 'Event'] as const;

export function News() {
  const [articles, setArticles] = useState<NewsItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [category, setCategory] = useState<string>('All');
  const containerRef = useRef<HTMLDivElement>(null);
  const gridRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    fetchNews();
  }, []);

  // Staggered reveal for cards when articles load
  useEffect(() => {
    if (!gridRef.current || articles.length === 0) return;
    const cards = gridRef.current.querySelectorAll('.news-card');
    gsap.from(cards, {
      opacity: 0,
      y: 30,
      scale: 0.97,
      stagger: 0.06,
      duration: 0.5,
      ease: 'power2.out',
      delay: 0.15,
    });
  }, [articles, category]);

  const fetchNews = async () => {
    setLoading(true);
    try {
      const data = await ipc.news.get();
      setArticles(data ?? []);
    } catch {
      setArticles([]);
    } finally {
      setLoading(false);
    }
  };

  const filtered = category === 'All'
    ? articles
    : articles.filter(a => a.category?.toLowerCase() === category.toLowerCase());

  const featured = filtered[0];
  const rest = filtered.slice(1);

  return (
    <div ref={containerRef} className="flex flex-col h-full overflow-y-auto custom-scrollbar">
      {/* Header */}
      <div className="px-8 pt-7 pb-4 flex items-center justify-between">
        <div className="flex items-center gap-3">
          <div
            className="w-10 h-10 rounded-xl flex items-center justify-center"
            style={{ background: 'linear-gradient(135deg, var(--accent), #a78bfa)', boxShadow: '0 4px 15px var(--accent-glow)' }}
          >
            <Newspaper size={20} color="white" />
          </div>
          <div>
            <h1 className="text-2xl font-bold" style={{ color: 'var(--text-primary)' }}>News</h1>
            <p className="text-xs" style={{ color: 'var(--text-muted)' }}>
              {articles.length} articles
            </p>
          </div>
        </div>

        <button
          onClick={fetchNews}
          disabled={loading}
          className="flex items-center gap-2 px-4 py-2 rounded-xl text-xs font-medium transition-all duration-200 hover:scale-105"
          style={{
            backgroundColor: 'var(--bg-light)',
            color: 'var(--text-secondary)',
            border: '1px solid var(--glass-border)',
          }}
        >
          <RefreshCw size={14} className={loading ? 'animate-spin' : ''} />
          Refresh
        </button>
      </div>

      {/* Category pills */}
      <div className="px-8 flex gap-2 mb-6">
        {CATEGORIES.map((cat) => (
          <button
            key={cat}
            onClick={() => setCategory(cat)}
            className="px-4 py-1.5 rounded-full text-xs font-medium transition-all duration-200"
            style={{
              backgroundColor: category === cat ? 'var(--accent)' : 'var(--bg-light)',
              color: category === cat ? 'white' : 'var(--text-secondary)',
              border: `1px solid ${category === cat ? 'var(--accent)' : 'var(--glass-border)'}`,
              boxShadow: category === cat ? '0 2px 12px var(--accent-glow)' : 'none',
            }}
          >
            {cat}
          </button>
        ))}
      </div>

      {/* Content */}
      <div className="px-8 pb-8 flex-1">
        {loading ? (
          <div className="flex items-center justify-center h-64">
            <div className="flex flex-col items-center gap-3">
              <RefreshCw size={32} className="animate-spin" style={{ color: 'var(--accent)' }} />
              <span className="text-sm" style={{ color: 'var(--text-muted)' }}>Loading news...</span>
            </div>
          </div>
        ) : filtered.length === 0 ? (
          <div className="flex items-center justify-center h-64">
            <div className="flex flex-col items-center gap-3">
              <Newspaper size={32} style={{ color: 'var(--text-muted)' }} />
              <span className="text-sm" style={{ color: 'var(--text-muted)' }}>No articles found</span>
            </div>
          </div>
        ) : (
          <div ref={gridRef}>
            {/* Featured article */}
            {featured && (
              <FeaturedCard article={featured} />
            )}

            {/* Grid */}
            {rest.length > 0 && (
              <div className="grid grid-cols-2 gap-4 mt-5">
                {rest.map((article, i) => (
                  <NewsCard key={article.url ?? i} article={article} index={i} />
                ))}
              </div>
            )}
          </div>
        )}
      </div>
    </div>
  );
}

function FeaturedCard({ article }: { article: NewsItem }) {
  const cardRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!cardRef.current) return;
    gsap.from(cardRef.current, {
      opacity: 0,
      y: 20,
      duration: 0.6,
      ease: 'power3.out',
      delay: 0.1,
    });
  }, []);

  return (
    <div
      ref={cardRef}
      className="news-card relative rounded-2xl overflow-hidden cursor-pointer group"
      style={{
        background: `linear-gradient(180deg, transparent 40%, var(--bg-darkest) 100%)${article.coverImageUrl ? `, url(${article.coverImageUrl}) center/cover` : ''}`,
        backgroundColor: 'var(--bg-medium)',
        minHeight: '240px',
        border: '1px solid var(--glass-border)',
      }}
      onClick={() => article.url && ipc.browser.open(article.url)}
    >
      {/* Overlay gradient */}
      <div className="absolute inset-0" style={{ background: 'linear-gradient(180deg, rgba(10,10,15,0.2) 0%, rgba(10,10,15,0.85) 70%)' }} />

      {/* Featured badge */}
      <div className="absolute top-4 left-4 z-10 flex items-center gap-1.5 px-3 py-1 rounded-full text-[10px] font-bold uppercase tracking-wider"
        style={{ background: 'linear-gradient(135deg, var(--accent), #a78bfa)', color: 'white' }}>
        <Star size={10} />
        Featured
      </div>

      {/* Content */}
      <div className="absolute bottom-0 left-0 right-0 p-6 z-10">
        {article.category && (
          <span className="inline-block px-2.5 py-0.5 rounded-md text-[10px] font-semibold uppercase mb-2"
            style={{ backgroundColor: 'rgba(124,92,252,0.2)', color: 'var(--accent)' }}>
            {article.category}
          </span>
        )}
        <h2 className="text-xl font-bold mb-2 group-hover:translate-x-1 transition-transform" style={{ color: 'var(--text-primary)' }}>
          {article.title}
        </h2>
        {article.description && (
          <p className="text-sm line-clamp-2 mb-3" style={{ color: 'var(--text-secondary)' }}>
            {article.description}
          </p>
        )}
        <div className="flex items-center gap-4">
          {article.publishDate && (
            <span className="flex items-center gap-1.5 text-[11px]" style={{ color: 'var(--text-muted)' }}>
              <Clock size={12} />
              {formatDate(article.publishDate)}
            </span>
          )}
          <span className="flex items-center gap-1.5 text-[11px]" style={{ color: 'var(--accent)' }}>
            <ExternalLink size={12} />
            Read more
          </span>
        </div>
      </div>
    </div>
  );
}

function NewsCard({ article, index }: { article: NewsItem; index: number }) {
  return (
    <GlassCard className="news-card p-5 cursor-pointer group" delay={index * 0.05}>
      <div
        onClick={() => article.url && ipc.browser.open(article.url)}
        className="flex flex-col h-full"
      >
        {/* Category & time */}
        <div className="flex items-center justify-between mb-3">
          {article.category ? (
            <span className="px-2 py-0.5 rounded-md text-[10px] font-semibold uppercase"
              style={{ backgroundColor: 'rgba(124,92,252,0.12)', color: 'var(--accent)' }}>
              {article.category}
            </span>
          ) : <span />}
          <TrendingUp size={14} style={{ color: 'var(--text-muted)' }} />
        </div>

        {/* Title */}
        <h3 className="text-sm font-semibold mb-2 line-clamp-2 group-hover:translate-x-0.5 transition-transform"
          style={{ color: 'var(--text-primary)' }}>
          {article.title}
        </h3>

        {/* Description */}
        {article.description && (
          <p className="text-xs line-clamp-2 mb-3 flex-1" style={{ color: 'var(--text-secondary)' }}>
            {article.description}
          </p>
        )}

        {/* Footer */}
        <div className="flex items-center justify-between mt-auto pt-3"
          style={{ borderTop: '1px solid var(--glass-border)' }}>
          {article.publishDate && (
            <span className="flex items-center gap-1 text-[11px]" style={{ color: 'var(--text-muted)' }}>
              <Clock size={11} />
              {formatDate(article.publishDate)}
            </span>
          )}
          <ExternalLink size={12} className="opacity-0 group-hover:opacity-100 transition-opacity"
            style={{ color: 'var(--accent)' }} />
        </div>
      </div>
    </GlassCard>
  );
}

function formatDate(dateStr: string): string {
  try {
    return new Date(dateStr).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
  } catch {
    return dateStr;
  }
}
