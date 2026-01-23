import i18n from 'i18next';
import { initReactI18next } from 'react-i18next';
import { invoke } from "@tauri-apps/api/core";
import supportedLanguages from './locales/languages.json';
import { syncTrayLanguage } from "./utils/tray";

const locales = import.meta.glob('./locales/*.json', { eager: true });
const resources: any = {};

for (const path in locales) {
const matched = path.match(/([a-z]{2}(-[A-Z]{2})?)\.json$/);
  if (matched) {
    const langCode = matched[1];
    const isDefined = supportedLanguages.find(l => l.code === langCode);
    if (isDefined) {
       const module: any = locales[path];
       resources[langCode] = { translation: module.default || module };
    }
  }
}

export const setupI18n = async () => {
  let initialLang = 'en-US';

  try {
    const config: any = await invoke("get_app_config");
    if (config && config.language) {
      initialLang = config.language;
    }
  } catch (error) {
    console.error("Dil ayarı yüklenemedi, varsayılan (en) kullanılıyor:", error);
  }

  await i18n
    .use(initReactI18next)
    .init({
      resources,
      lng: initialLang,
      fallbackLng: 'en-US',
      supportedLngs: supportedLanguages.map(l => l.code),
      interpolation: {
        escapeValue: false
      }
    });

    await syncTrayLanguage(i18n.t);

  return i18n;
};

export default i18n;