import { listen } from '@tauri-apps/api/event';
import { useState, useEffect, useMemo } from "react";
import { invoke } from "@tauri-apps/api/core";
import { Settings, Cpu } from "lucide-react";
import { useTranslation } from "react-i18next";

import { TitleBar } from "./components/layout/TitleBar";
import { PowerButton } from "./components/features/PowerButton";
import { SettingsModal } from "./components/settings/SettingsModal";
import { AppConfig, AppError } from "./types";
import { syncTrayLanguage } from "./utils/tray";
import { useToast } from "./context/ToastContext";
import "./App.css";

function App() {
  const { t } = useTranslation();
  const toast = useToast();

  const [isRunning, setIsRunning] = useState(false);
  const [isProcessing, setIsProcessing] = useState(false);
  const [showSettings, setShowSettings] = useState(false);
  const [appConfig, setAppConfig] = useState<AppConfig | null>(null);

  const refreshConfig = async () => {
    try {
      const config = await invoke<AppConfig>("get_app_config");
      setAppConfig(config);
    } catch (error) {
      console.error(error);
    }
  };

  useEffect(() => {
    async function checkStatus() {
      const running = await invoke<boolean>("is_engine_running");
      setIsRunning(running);
      syncTrayLanguage(t);
    }
    checkStatus();
    refreshConfig();

    const unlistenPromise = listen<boolean>('engine-status', (event) => {
      console.log("Motor durumu değişti:", event.payload);
      setIsRunning(event.payload);
      syncTrayLanguage(t);
    });

    return () => {
      unlistenPromise.then(unlisten => unlisten());
    };
  }, [t]);

  const toggleEngine = async () => {
    if (isProcessing) return;

    setIsProcessing(true);

    try {
      if (isRunning) {
        await invoke("stop_engine");
      } else {
        await refreshConfig();
        await invoke("start_engine");
      }
    } catch (err: unknown) {
      const error = err as AppError;
      console.error("Motor hatası:", error);

      const translationKey = `errors.${error.code}`;
      let userMessage = t(translationKey);

      if (userMessage === translationKey) {
        userMessage = `${t('errors.ERR_UNKNOWN')} (${error.message || error.code})`;
      }

      toast.error(userMessage);
    } finally {
      setIsProcessing(false);
    }
  };

  const handleSettingsClose = () => {
    setShowSettings(false);
    refreshConfig();
  };

  const activeEngineName = useMemo(() => {
    if (!appConfig) return t('app.loading');

    if (appConfig.activeEngineId === 'internal') {
      return `IHateDPI: ${t('app.native_mode')}`;
    }

    const currentEngine = appConfig.externalEngines.find(e => e.id === appConfig.activeEngineId);

    if (currentEngine) {
      let details = `${t('app.default')}`;

      if (currentEngine.scriptPath) {
        details = currentEngine.scriptPath.split(/[\\/]/).pop() || currentEngine.scriptPath;
      } else if (currentEngine.manualArgs) {
        details = t('settings.external_engine.manual_args');
      }

      return `${currentEngine.name}: ${details}`;
    }

    return t('app.no_engine_selected');
  }, [appConfig, t]);


  return (
    <div className="relative w-screen h-screen bg-cyber-dark overflow-hidden flex flex-col border border-white/5 shadow-2xl rounded-xl font-sans select-none">

      <div className={`absolute top-[-50%] left-[-20%] w-[600px] h-[600px] rounded-full blur-[120px] opacity-15 transition-colors duration-1000 pointer-events-none ${isRunning ? 'bg-cyber-primary' : 'bg-cyber-danger'}`}></div>
      <div className="absolute bottom-[-20%] right-[-10%] w-[400px] h-[400px] rounded-full bg-blue-900 blur-[100px] opacity-10 pointer-events-none"></div>

      <TitleBar isRunning={isRunning} minimizeToTray={appConfig?.minimizeToTray ?? true} />

      <div className="flex-1 flex items-center justify-center">
        <PowerButton isRunning={isRunning} isProcessing={isProcessing} onClick={toggleEngine} />
      </div>

      <div className="relative z-20 mx-4 mb-4">
        <div className="bg-white/5 backdrop-blur-md border border-white/5 rounded-xl p-2 flex items-center justify-between shadow-lg">

          <div className="flex items-center gap-3 pl-2">
            <div className={`w-8 h-8 rounded-lg flex items-center justify-center bg-white/5 border border-white/5 transition-colors ${isRunning ? 'border-cyber-primary/30 bg-cyber-primary/10' : ''}`}>
              <Cpu size={16} className={isRunning ? "text-cyber-primary" : "text-white/40"} />
            </div>
            <div className="flex flex-col">
              <span className="text-[9px] font-bold text-white/30 uppercase tracking-wider">
                {t('app.active_module')}
              </span>
              <span className="text-xs text-white/90 font-medium truncate max-w-[200px]" title={activeEngineName}>
                {activeEngineName}
              </span>
            </div>
          </div>

          <button
            onClick={() => setShowSettings(true)}
            disabled={isRunning}
            className={`p-2.5 rounded-lg transition-all duration-200 border border-transparent ${isRunning ? 'opacity-30 cursor-not-allowed' : 'hover:bg-white/10 hover:border-white/5 text-white/60 hover:text-white'}`}
          >
            <Settings size={18} />
          </button>
        </div>
      </div>

      {showSettings && <SettingsModal onClose={handleSettingsClose} />}
    </div>
  );
}

export default App;