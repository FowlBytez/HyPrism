import React, { Suspense, lazy } from 'react';
import { motion } from 'framer-motion';
import { useTranslation } from 'react-i18next';
import { PageContainer } from '@/components/ui/PageContainer';
import { SettingsHeader } from '@/components/ui/SettingsHeader';

const SettingsModal = lazy(() => import('../components/SettingsModal').then(m => ({ default: m.SettingsModal })));

const pageVariants = {
  initial: { opacity: 0, y: 12 },
  animate: { opacity: 1, y: 0 },
  exit: { opacity: 0, y: -12 },
};

interface SettingsPageProps {
  launcherBranch: string;
  onLauncherBranchChange: (branch: string) => void;
  rosettaWarning?: { message: string; command: string; tutorialUrl?: string } | null;
  onBackgroundModeChange?: (mode: string) => void;
  onAccentColorChange?: (color: string) => void;
  onInstanceDeleted?: () => void;
  onNavigateToMods?: () => void;
  onAuthSettingsChange?: () => void;
  isGameRunning?: boolean;
  onMovingDataChange?: (isMoving: boolean) => void;
}

export const SettingsPage: React.FC<SettingsPageProps> = (props) => {
  const { t } = useTranslation();

  return (
    <motion.div
      variants={pageVariants}
      initial="initial"
      animate="animate"
      exit="exit"
      transition={{ duration: 0.3, ease: 'easeOut' }}
      className="h-full w-full"
    >
      <PageContainer contentClassName="h-full">
        <div className="h-full flex flex-col">
          <div className="flex-shrink-0 mb-4">
            <SettingsHeader title={t('settings.title')} />
          </div>
          <div className="flex-1 min-h-0">
            <Suspense fallback={
              <div className="flex items-center justify-center h-full">
                <div className="w-8 h-8 border-2 border-white/20 border-t-white rounded-full animate-spin" />
              </div>
            }>
              <SettingsModal
                onClose={() => {}}
                launcherBranch={props.launcherBranch}
                onLauncherBranchChange={props.onLauncherBranchChange}
                rosettaWarning={props.rosettaWarning}
                onBackgroundModeChange={props.onBackgroundModeChange}
                onAccentColorChange={props.onAccentColorChange}
                onInstanceDeleted={props.onInstanceDeleted}
                onAuthSettingsChange={props.onAuthSettingsChange}
                pageMode={true}
                isGameRunning={props.isGameRunning}
                onMovingDataChange={props.onMovingDataChange}
              />
            </Suspense>
          </div>
        </div>
      </PageContainer>
    </motion.div>
  );
};
