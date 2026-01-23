import { useEffect, useState } from "react";
import { invoke } from "@tauri-apps/api/core";
import { Network, Split, Biohazard, FileJson, Ghost, ChevronDown, Loader2 } from "lucide-react";
import { useTranslation } from "react-i18next";
import { Switch } from "../ui/Switch";
import { Input } from "../ui/Input";
import { EngineConfig } from "../../types";


interface Props {
    onConfigChange: (config: EngineConfig) => void;
}

function ConfigSection({ title, icon: Icon, colorClass, children, defaultOpen = false }: any) {
    const [isOpen, setIsOpen] = useState(defaultOpen);

    return (
        <div className="bg-white/[0.03] border border-white/5 rounded-xl overflow-hidden transition-all duration-300 hover:border-white/10 hover:bg-white/[0.05]">
            <button
                onClick={() => setIsOpen(!isOpen)}
                className="w-full flex items-center justify-between p-3.5 text-left outline-none"
            >
                <div className="flex items-center gap-3">
                    <div className={`p-1.5 rounded-lg bg-black/20 ${colorClass}`}>
                        <Icon size={14} />
                    </div>
                    <span className="text-[11px] font-bold text-white tracking-widest uppercase">{title}</span>
                </div>
                <ChevronDown size={14} className={`text-white/30 transition-transform duration-300 ${isOpen ? 'rotate-180' : ''}`} />
            </button>

            <div className={`transition-all duration-300 ease-in-out ${isOpen ? 'max-h-[600px] opacity-100' : 'max-h-0 opacity-0'}`}>
                <div className="p-4 pt-0 space-y-3 border-t border-white/5">
                    <div className="h-2"></div>
                    {children}
                </div>
            </div>
        </div>
    );
}

export function SettingsIHateDPI({ onConfigChange }: Props) {
    const { t } = useTranslation();
    const [config, setConfig] = useState<EngineConfig | null>(null);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        async function fetchData() {
            try {
                const data = await invoke<EngineConfig>("get_engine_config");
                setConfig(data);
                onConfigChange(data);
            } catch (error) {
                console.error("Config could not load", error);
            } finally {
                setLoading(false);
            }
        }
        fetchData();
    }, []);

    const update = (key: keyof EngineConfig, value: any) => {
        if (!config) return;

        if (typeof config[key] === 'number' && typeof value === 'string') {
            const parsed = parseInt(value);
            value = isNaN(parsed) ? 0 : parsed;
        }

        const newConfig = { ...config, [key]: value };
        setConfig(newConfig);
        onConfigChange(newConfig);
    };

    if (loading || !config) {
        return (
            <div className="flex flex-col items-center justify-center py-10 space-y-3 opacity-50">
                <Loader2 className="animate-spin text-cyan-500" size={24} />
                <span className="text-xs text-white tracking-widest uppercase">
                    {t('settings.general.loading')}
                </span>
            </div>
        );
    }

    return (
        <div className="space-y-3 pb-6">

            <ConfigSection title={t('settings.ihatedpi.connection_dns')} icon={Network} colorClass="text-cyan-400" defaultOpen={true}>
                <Switch
                    label={t('settings.ihatedpi.doh')}
                    checked={config.isDohEnabled}
                    onChange={(v) => update('isDohEnabled', v)}
                />

                <div className={`transition-all duration-300 overflow-hidden ${config.isDohEnabled ? 'max-h-20 opacity-100' : 'max-h-0 opacity-0'}`}>
                    <Input
                        label={t('settings.ihatedpi.doh_url')}
                        value={config.dohProviderUrl}
                        onChange={(e) => update('dohProviderUrl', e.target.value)}
                    />
                </div>

                <div className="h-px bg-white/5 my-2"></div>

                <Switch label={t('settings.ihatedpi.block_quic')} checked={config.blockQuic} onChange={(v) => update('blockQuic', v)} />
                <Input
                    label={t('settings.ihatedpi.max_payload')} type="number"
                    value={config.maxPayloadSize} onChange={(e) => update('maxPayloadSize', e.target.value)}
                />
            </ConfigSection>

            <ConfigSection title={t('settings.ihatedpi.fragmentation')} icon={Split} colorClass="text-purple-400">
                <Switch label={t('settings.ihatedpi.reverse_frag')} checked={config.reverseFragmentation} onChange={(v) => update('reverseFragmentation', v)} />
                <Switch label={t('settings.ihatedpi.auto_split')} checked={config.autoSplitSni} onChange={(v) => update('autoSplitSni', v)} />

                <div className="grid grid-cols-2 gap-3 pt-1">
                    <Input label={t('settings.ihatedpi.https_frag')} type="number" value={config.fragmentHttps} onChange={(e) => update('fragmentHttps', e.target.value)} />
                    <Input label={t('settings.ihatedpi.http_frag')} type="number" value={config.fragmentHttp} onChange={(e) => update('fragmentHttp', e.target.value)} />
                </div>
            </ConfigSection>

            <ConfigSection title={t('settings.ihatedpi.buffer_poisoning')} icon={Biohazard} colorClass="text-green-400">
                <Switch label={t('settings.ihatedpi.enable_poisoning')} checked={config.bufferPoisoning} onChange={(v) => update('bufferPoisoning', v)} />

                <div className={`transition-all duration-300 ${config.bufferPoisoning ? 'opacity-100' : 'opacity-40 pointer-events-none'}`}>
                    <div className="grid grid-cols-3 gap-2 pt-2 pb-2">
                        <Input label={t('settings.ihatedpi.size')} type="number" value={config.junkPacketSize} onChange={(e) => update('junkPacketSize', e.target.value)} />
                        <Input label={t('settings.ihatedpi.count')} type="number" value={config.junkPacketCount} onChange={(e) => update('junkPacketCount', e.target.value)} />
                        <Input label={t('settings.ihatedpi.ttl')} type="number" value={config.junkPacketTtl} onChange={(e) => update('junkPacketTtl', e.target.value)} />
                    </div>
                    <Switch label={t('settings.ihatedpi.bad_checksum')} checked={config.junkPacketBadChecksum} onChange={(v) => update('junkPacketBadChecksum', v)} />
                    <Switch label={t('settings.ihatedpi.bad_sequence')} checked={config.junkPacketBadSequence} onChange={(v) => update('junkPacketBadSequence', v)} />
                </div>
            </ConfigSection>

            <ConfigSection title={t('settings.ihatedpi.http_header')} icon={FileJson} colorClass="text-yellow-400">
                <Switch label={t('settings.ihatedpi.mix_host')} checked={config.mixHost} onChange={(v) => update('mixHost', v)} />
                <Switch label={t('settings.ihatedpi.host_no_space')} checked={config.hostNoSpace} onChange={(v) => update('hostNoSpace', v)} />
                <Switch label={t('settings.ihatedpi.additional_space')} checked={config.additionalSpace} onChange={(v) => update('additionalSpace', v)} />
            </ConfigSection>

            <ConfigSection title={t('settings.ihatedpi.fake_packet')} icon={Ghost} colorClass="text-rose-400">
                <Switch label={t('settings.ihatedpi.bad_sequence')} checked={config.badSequence} onChange={(v) => update('badSequence', v)} />
                <Switch label={t('settings.ihatedpi.bad_checksum')} checked={config.badCheckSum} onChange={(v) => update('badCheckSum', v)} />
                <div className="grid grid-cols-2 gap-3 pt-2">
                    <Input label={t('settings.ihatedpi.ttl')} type="number" value={config.fakePacketTtl} onChange={(e) => update('fakePacketTtl', e.target.value)} />
                    <Input label={t('settings.ihatedpi.count')} type="number" value={config.fakeRequestResendCount} onChange={(e) => update('fakeRequestResendCount', e.target.value)} />
                </div>
            </ConfigSection>

        </div>
    );
}