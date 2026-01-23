import { open } from '@tauri-apps/plugin-dialog';
import { FolderOpen, Play, X } from "lucide-react";
import { useTranslation } from "react-i18next";
import { Input } from "../ui/Input";
import { ExternalEngine } from "../../types";

interface Props {
  engine: ExternalEngine;
  onUpdate: (field: keyof ExternalEngine, value: any) => void;
}

export function SettingsExternalEngine({ engine, onUpdate }: Props) {
  const { t } = useTranslation();

  const browseFile = async (field: 'exePath' | 'scriptPath', extensions: string[]) => {
    try {
      const selected = await open({
        multiple: false,
        filters: [{ name: field === 'exePath' ? 'Executable' : 'Script', extensions }]
      });

      if (selected && typeof selected === 'string') {
        onUpdate(field, selected);
      }
    } catch (err) {
      console.error("Dosya seçimi hatası:", err);
    }
  };

  return (
    <div className="space-y-4 pr-1 animate-in slide-in-from-right-4 fade-in duration-300">

      <div className="flex items-center gap-2 mb-4">
        <div className="p-2 rounded-lg bg-purple-500/20 text-purple-400">
          <Play size={20} />
        </div>
        <div>
          <h3 className="text-sm font-bold text-white uppercase tracking-widest">{t('settings.external_engine.engine_configuration')}</h3>
          <p className="text-[10px] text-white/40">{t('settings.external_engine.engine_configuration_desc')}</p>
        </div>
      </div>

      <Input
        label={t('settings.external_engine.engine_name')}
        value={engine.name}
        onChange={(e) => onUpdate('name', e.target.value)}
        className="bg-black/40"
      />

      <div className="space-y-1.5">
        <label className="text-[10px] font-bold text-white/30 uppercase ml-1">
          {t('settings.external_engine.exe_path')}
        </label>
        <div className="flex gap-2">
          <div className="flex-1 bg-black/40 border border-white/5 rounded-lg px-3 py-2 text-xs text-white/70 truncate font-mono select-all">
            {engine.exePath || <span className="text-white/20 italic">{t('settings.external_engine.not_selected')}</span>}
          </div>
          <button
            onClick={() => browseFile('exePath', ['exe'])}
            className="px-3 rounded-lg bg-white/10 hover:bg-white/20 border border-white/5 transition-colors text-white/70"
            title="Dosya Seç"
          >
            <FolderOpen size={16} />
          </button>
        </div>
      </div>

      <div className="space-y-1.5">
        <label className="text-[10px] font-bold text-white/30 uppercase ml-1">
          {t('settings.external_engine.script_path')}
        </label>
        <div className="flex gap-2">
          <div className="flex-1 bg-black/40 border border-white/5 rounded-lg px-3 py-2 text-xs text-white/70 truncate font-mono select-all">
            {engine.scriptPath || <span className="text-white/20 italic">{t('settings.external_engine.script_path_placeholder')}</span>}
          </div>

          {engine.scriptPath && (
            <button
              onClick={() => onUpdate('scriptPath', null)}
              className="px-3 rounded-lg bg-red-500/10 hover:bg-red-500/20 border border-red-500/10 hover:border-red-500/30 transition-colors text-red-400"
              title={t('settings.external_engine.clear_script')}
            >
              <X size={16} />
            </button>
          )}

          <button
            onClick={() => browseFile('scriptPath', ['cmd', 'bat'])}
            className="px-3 rounded-lg bg-white/10 hover:bg-white/20 border border-white/5 transition-colors text-white/70"
            title={t('settings.external_engine.select_script')}
          >
            <FolderOpen size={16} />
          </button>
        </div>
      </div>

      <Input
        label={t('settings.external_engine.manual_args')}
        value={engine.manualArgs || ""}
        placeholder={t('settings.external_engine.manual_args_placeholder')}
        onChange={(e) => onUpdate('manualArgs', e.target.value)}
        className="bg-black/40 font-mono text-orange-200/80"
      />
    </div>
  );
}