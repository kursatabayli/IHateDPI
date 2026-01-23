import { getCurrentWindow } from "@tauri-apps/api/window";
import { Minimize2, X, ShieldCheck } from "lucide-react";
import { useTranslation } from "react-i18next";

interface TitleBarProps {
  isRunning: boolean;
  minimizeToTray: boolean;
}

export function TitleBar({ isRunning, minimizeToTray }: TitleBarProps) {
  const { t } = useTranslation();

  const handleMinimize = async () => {
    const appWindow = getCurrentWindow();

    if (minimizeToTray) {
      await appWindow.hide();
    } else {
      await appWindow.minimize();
    }
  };

  return (
    <div data-tauri-drag-region className="relative z-50 h-10 flex items-center justify-between px-4 bg-white/5 backdrop-blur-md border-b border-cyber-border">

      <div className="flex items-center gap-2 pointer-events-none select-none">
        <ShieldCheck size={14} className={`transition-colors duration-500 ${isRunning ? "text-cyber-primary" : "text-white/20"}`} />
        <span className="text-[10px] font-bold tracking-widest uppercase text-white/40">
          {t('app.title')}
        </span>
      </div>

      <div className="flex gap-1.5">
        <button
          onClick={handleMinimize}
          className="p-1.5 rounded hover:bg-white/5 text-white/40 hover:text-white transition-colors"
          title={minimizeToTray ? (t('tray.hide')) : (t('tray.minimize'))}
        >
          <Minimize2 size={14} />
        </button>

        <button
          onClick={() => getCurrentWindow().close()}
          className="p-1.5 rounded hover:bg-cyber-danger/20 hover:text-cyber-danger text-white/40 transition-colors"
          title={t('tray.quit') || "Quit"}
        >
          <X size={14} />
        </button>
      </div>
    </div>
  );
}