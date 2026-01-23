import { Plus, Trash2, Zap, Play } from "lucide-react";
import { useTranslation } from "react-i18next";
import { SettingsIHateDPI } from "./SettingsIHateDPI";
import { SettingsExternalEngine } from "./SettingsExternalEngine";
import { AppConfig, EngineConfig, ExternalEngine } from "../../types";

interface Props {
  appConfig: AppConfig;
  onAppConfigChange: (cfg: AppConfig) => void;
  onIhateConfigChange: (cfg: EngineConfig) => void;
}

export function SettingsEngine({ appConfig, onAppConfigChange, onIhateConfigChange }: Props) {
  const { t } = useTranslation();

  const activeExternalEngine = appConfig.externalEngines.find(e => e.id === appConfig.activeEngineId);

  const handleAddEngine = async () => {
    try {
      const newId = `eng_${Date.now()}`;
      const newEngine: ExternalEngine = {
        id: newId,
        name: t('settings.external_engine.new_engine'),
        exePath: "",
        scriptPath: null,
        manualArgs: null
      };

      onAppConfigChange({
        ...appConfig,
        externalEngines: [...appConfig.externalEngines, newEngine],
        activeEngineId: newId
      });

    } catch (err) {
      console.error("Motor ekleme hatası:", err);
    }
  };

  const handleRemoveEngine = (e: React.MouseEvent, id: string) => {
    e.stopPropagation();
    const filtered = appConfig.externalEngines.filter(eng => eng.id !== id);

    let newActiveId = appConfig.activeEngineId;
    if (appConfig.activeEngineId === id) {
      newActiveId = "internal";
    }

    onAppConfigChange({
      ...appConfig,
      externalEngines: filtered,
      activeEngineId: newActiveId
    });
  };

  const updateActiveEngineField = (field: keyof ExternalEngine, value: any) => {
    if (!activeExternalEngine) return;

    const updatedEngines = appConfig.externalEngines.map(eng => {
      if (eng.id === activeExternalEngine.id) {
        return { ...eng, [field]: value };
      }
      return eng;
    });

    onAppConfigChange({ ...appConfig, externalEngines: updatedEngines });
  };

  return (
    <div className="flex flex-col h-full overflow-hidden">

      <div className="flex-none h-14 flex items-center gap-2 overflow-x-auto custom-scrollbar border-b border-white/5 bg-black/10 -mx-1 px-2">

        <button
          onClick={() => onAppConfigChange({ ...appConfig, activeEngineId: 'internal' })}
          className={`
            relative flex items-center gap-2 px-3 py-1.5 rounded-lg border transition-all whitespace-nowrap select-none
            ${appConfig.activeEngineId === 'internal'
              ? 'bg-cyan-500/10 border-cyan-500/50 text-cyan-400 shadow-[0_0_10px_rgba(34,211,238,0.15)]'
              : 'bg-white/5 border-white/5 text-white/50 hover:bg-white/10 hover:text-white'
            }
          `}
        >
          <Zap size={14} className={appConfig.activeEngineId === 'internal' ? "fill-cyan-400/20" : ""} />
          <span className="text-xs font-bold tracking-wider">IHateDPI</span>
          {appConfig.activeEngineId === 'internal' && <div className="w-1.5 h-1.5 rounded-full bg-cyan-400 ml-1 animate-pulse" />}
        </button>

        <div className="w-px h-6 bg-white/10 mx-1 flex-shrink-0"></div>

        {appConfig.externalEngines.map(engine => {
          const isActive = appConfig.activeEngineId === engine.id;
          return (
            <div
              key={engine.id}
              onClick={() => onAppConfigChange({ ...appConfig, activeEngineId: engine.id })}
              className={`
                group relative flex items-center gap-2 px-3 py-1.5 rounded-lg border cursor-pointer transition-all whitespace-nowrap select-none
                ${isActive
                  ? 'bg-purple-500/10 border-purple-500/50 text-purple-400 shadow-[0_0_10px_rgba(168,85,247,0.15)] pr-8'
                  : 'bg-white/5 border-white/5 text-white/50 hover:bg-white/10 hover:text-white'
                }
              `}
            >
              <Play size={14} className={isActive ? "fill-purple-400/20" : ""} />
              <span className="text-xs font-medium max-w-[100px] truncate">
                {engine.name || "İsimsiz"}
              </span>

              <button
                onClick={(e) => handleRemoveEngine(e, engine.id)}
                className={`
                  absolute right-1 p-1 rounded-md hover:bg-red-500/20 hover:text-red-400 transition-all
                  ${isActive ? 'opacity-100' : 'opacity-0 group-hover:opacity-100'}
                `}
                title={t('settings.external_engine.delete_engine')}
              >
                <Trash2 size={12} />
              </button>
            </div>
          );
        })}

        <button
          onClick={handleAddEngine}
          className="flex-shrink-0 w-8 h-8 flex items-center justify-center rounded-lg border border-dashed border-white/20 text-white/30 hover:text-white hover:border-white/50 hover:bg-white/5 transition-all"
          title="Yeni Motor Ekle"
        >
          <Plus size={16} />
        </button>
      </div>

      <div className="flex-1 overflow-y-auto custom-scrollbar pt-4 px-1">
        {appConfig.activeEngineId === 'internal' ? (
          <div className="animate-in fade-in slide-in-from-bottom-2 duration-300">
            <SettingsIHateDPI onConfigChange={onIhateConfigChange} />
          </div>
        ) : (
          activeExternalEngine ? (
            <div className="animate-in fade-in slide-in-from-bottom-2 duration-300">
              <SettingsExternalEngine
                engine={activeExternalEngine}
                onUpdate={updateActiveEngineField}
              />
            </div>
          ) : (
            <div className="flex flex-col items-center justify-center h-full opacity-30">
              <div className="p-4 rounded-full bg-white/5 mb-3">
                <Play size={32} />
              </div>
              <p className="text-xs font-bold uppercase tracking-widest">{t('settings.general.select_engine')}</p>
            </div>
          )
        )}
      </div>

    </div>
  );
}