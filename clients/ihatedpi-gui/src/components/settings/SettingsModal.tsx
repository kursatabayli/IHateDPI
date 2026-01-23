import { useState, useEffect } from "react";
import { invoke } from "@tauri-apps/api/core";
import { Save, Settings2, Sliders, Box, Check } from "lucide-react";
import { useTranslation } from "react-i18next";
import { SettingsEngine } from "./SettingsEngine";
import { SettingsGeneral } from "./SettingsGeneral";
import { AppConfig, EngineConfig, AppError } from "../../types";
import { syncTrayLanguage } from "../../utils/tray";
import { useToast } from "../../context/ToastContext";

interface Props {
    onClose: () => void;
}

type TabType = "general" | "engine";

export function SettingsModal({ onClose }: Props) {
    const { t, i18n } = useTranslation();
    const toast = useToast();

    const [activeTab, setActiveTab] = useState<TabType>("engine");
    const [appConfig, setAppConfig] = useState<AppConfig | null>(null);
    const [ihateConfig, setIhateConfig] = useState<EngineConfig | null>(null);

    const [saving, setSaving] = useState(false);
    const [success, setSuccess] = useState(false);

    useEffect(() => {
        async function loadData() {
            try {
                const mainCfg = await invoke<AppConfig>("get_app_config");
                setAppConfig(mainCfg);
            } catch (error) {
                console.error("Ayarlar yüklenirken hata:", error);
                toast.error(t('settings.errors.ERR_LOAD_CONFIG') || "Ayarlar yüklenemedi");
            }
        }
        loadData();
    }, []);

    useEffect(() => {
        if (success) {
            const timer = setTimeout(() => {
                setSuccess(false);
            }, 2000);
            return () => clearTimeout(timer);
        }
    }, [success]);

    const handleSave = async () => {
        if (!appConfig) return;
        setSaving(true);
        setSuccess(false);

        try {
            await invoke("save_app_config", { config: appConfig });

            if (ihateConfig) {
                await invoke("save_engine_config", { config: ihateConfig });
            }

            if (appConfig.language && appConfig.language !== i18n.language) {
                await i18n.changeLanguage(appConfig.language);
            }

            await syncTrayLanguage(i18n.t);

            setSuccess(true);
            toast.success(t('settings.general.saved'));

        } catch (err: unknown) {
            const error = err as AppError;
            console.error("Kaydetme hatası:", error);

            const translationKey = `errors.${error.code}`;
            let userMessage = t(translationKey);

            if (userMessage === translationKey) {
                userMessage = `${t('errors.ERR_UNKNOWN')} (${error.message || error.code})`;
            }
            toast.error(userMessage);
        } finally {
            setSaving(false);
        }
    };

    if (!appConfig) return null;

    return (
        <div className="absolute inset-0 z-50 flex flex-col bg-[#0a0a0f] animate-in slide-in-from-bottom-5 fade-in duration-300">

            <div className="shrink-0 flex items-center p-5 border-b border-white/5 bg-white/5 backdrop-blur-md">
                <Settings2 size={18} className="text-cyan-400 mr-2" />
                <h2 className="text-sm font-bold text-white tracking-[0.2em] uppercase">
                    {t('settings.general.title')}
                </h2>
            </div>

            <div className="flex-1 flex overflow-hidden">
                <div className="w-16 bg-black/20 border-r border-white/5 flex flex-col items-center py-4 gap-2">
                    <SidebarBtn
                        active={activeTab === "engine"}
                        onClick={() => setActiveTab("engine")}
                        icon={Sliders}
                        label={t('settings.sidebar.engine')}
                    />
                    <SidebarBtn
                        active={activeTab === "general"}
                        onClick={() => setActiveTab("general")}
                        icon={Box}
                        label={t('settings.sidebar.general')}
                    />
                </div>

                <div className="flex-1 overflow-hidden relative">
                    {activeTab === "engine" ? (
                        <SettingsEngine
                            appConfig={appConfig}
                            onAppConfigChange={setAppConfig}
                            onIhateConfigChange={setIhateConfig}
                        />
                    ) : (
                        <SettingsGeneral
                            config={appConfig}
                            onChange={setAppConfig}
                        />
                    )}
                </div>
            </div>

            <div className="shrink-0 p-4 bg-[#0a0a0f] border-t border-white/5 flex gap-3 shadow-[0_-10px_40px_rgba(0,0,0,0.8)] z-50">
                <button
                    onClick={onClose}
                    className="flex-1 py-3 rounded-lg border border-white/10 text-white/50 text-xs font-bold uppercase tracking-widest hover:bg-white/5 hover:text-white transition-all"
                >
                    {t('settings.general.back')}
                </button>

                <button
                    onClick={handleSave}
                    disabled={saving || success}
                    className={`
                        flex-1 py-3 rounded-lg text-[#0a0a0f] text-xs font-bold uppercase tracking-widest active:scale-[0.98] transition-all flex items-center justify-center gap-2
                        ${success
                            ? "bg-green-500 hover:bg-green-400 shadow-[0_0_20px_rgba(34,197,94,0.4)] cursor-default"
                            : "bg-cyan-500 hover:bg-cyan-400 hover:shadow-[0_0_20px_rgba(34,211,238,0.4)]"
                        }
                    `}
                >
                    {saving ? (
                        <span className="animate-pulse">{t('settings.general.saving')}</span>
                    ) : success ? (
                        <>
                            <Check size={16} />
                            <span>{t('settings.general.saved')}</span>
                        </>
                    ) : (
                        <>
                            <Save size={14} />
                            {t('settings.general.save')}
                        </>
                    )}
                </button>
            </div>
        </div>
    );
}

function SidebarBtn({ active, onClick, icon: Icon, label }: any) {
    return (
        <button
            onClick={onClick}
            className={`
                group relative w-10 h-10 flex items-center justify-center rounded-xl transition-all duration-300
                ${active
                    ? "bg-cyan-500/10 text-cyan-400 shadow-[0_0_15px_rgba(6,182,212,0.15)]"
                    : "text-white/30 hover:bg-white/5 hover:text-white"
                }
            `}
            title={label}
        >
            <Icon size={18} className={`transition-transform duration-300 ${active ? 'scale-110' : 'group-hover:scale-110'}`} />
            {active && (
                <div className="absolute left-0 top-2 bottom-2 w-0.5 bg-cyan-400 rounded-r-full"></div>
            )}
        </button>
    );
}