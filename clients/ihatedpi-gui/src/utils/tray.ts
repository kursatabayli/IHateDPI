import { invoke } from "@tauri-apps/api/core";
import { TFunction } from "i18next";

export async function syncTrayLanguage(t: TFunction) {
  try {
    await invoke("update_tray_lang", {
      locales: {
        start: t("tray.start"),
        stop: t("tray.stop"),
        quit: t("tray.quit"),
        show: t("tray.show"),
        hide: t("tray.hide"),
      },
    });
    console.log("Tray dili senkronize edildi.");
  } catch (error) {
    console.error("Tray dili güncellenemedi:", error);
  }
}