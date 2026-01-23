import { useTranslation } from "react-i18next";
import { Monitor, Globe } from "lucide-react";
import { Switch } from "../ui/Switch";
import { Select } from "../ui/Select";
import { AppConfig } from "../../types";
import supportedLanguages from "../../locales/languages.json";

interface Props {
  config: AppConfig;
  onChange: (newConfig: AppConfig) => void;
}

export function SettingsGeneral({ config, onChange }: Props) {
  const { t } = useTranslation();

  const languages = supportedLanguages.map((lang) => {
    return {
      value: lang.code,
      label: lang.name,
      icon: (
        <img
          src={`/flags/${lang.flagCode}.svg`}
          alt={lang.flagCode}
          className="w-5 h-4 rounded-sm object-cover"
        />
      )
    };
  });

  const currentLang = config.language || "en-US";

  const handleLangChange = (val: string) => {
    onChange({
      ...config,
      language: val
    });
  };

  return (
    <div className="space-y-6 p-1">

      <div className="space-y-3">
        <div className="flex items-center gap-2 mb-2">
          <Globe size={16} className="text-blue-400" />
          <h3 className="text-xs font-bold text-white/70 tracking-widest uppercase">
            {t('settings.general.language')}
          </h3>
        </div>
        <div className="bg-white/5 border border-white/5 rounded-xl p-4">
          <Select
            options={languages}
            value={currentLang}
            onChange={handleLangChange}
            placeholder={t('settings.general.select_lang')}
            className="w-full"
          />
          <p className="text-[10px] text-white/30 mt-2 ml-1">
            * {t('settings.general.restart_note')}
          </p>
        </div>
      </div>

      <div className="space-y-3">
        <div className="flex items-center gap-2 mb-2">
          <Monitor size={16} className="text-orange-400" />
          <h3 className="text-xs font-bold text-white/70 tracking-widest uppercase">
            {t('settings.general.system')}
          </h3>
        </div>
        <div className="bg-white/5 border border-white/5 rounded-xl p-4 space-y-4">
          <Switch
            label={t('settings.general.minimize_tray')}
            checked={config.minimizeToTray}
            onChange={(val) => onChange({ ...config, minimizeToTray: val })}
          />
        </div>
      </div>

    </div>
  );
}