import { Power, Loader2 } from "lucide-react";
import { useTranslation } from "react-i18next";

interface Props {
  isRunning: boolean;
  isProcessing: boolean;
  onClick: () => void;
}

export function PowerButton({ isRunning, isProcessing, onClick }: Props) {
  const { t } = useTranslation();

  const statusText = isProcessing
    ? (isRunning ? t('app.engine_stopping') : t('app.engine_starting'))
    : (isRunning ? t('app.engine_online') : t('app.engine_offline'));

  const statusColor = isProcessing
    ? "text-yellow-400"
    : (isRunning ? "text-cyber-primary" : "text-cyber-danger");

  return (
    <div className="relative flex flex-col items-center justify-center z-10">
      <div className={`absolute -top-16 flex flex-col items-center transition-all duration-500 opacity-100 translate-y-0`}>
        <span className={`text-[10px] font-bold tracking-[0.3em] uppercase ${statusColor} ${isRunning || isProcessing ? 'drop-shadow-[0_0_8px_rgba(250,204,21,0.5)]' : ''}`}>
          {statusText}
        </span>
      </div>

      <button
        onClick={onClick}
        disabled={isProcessing}
        className={`relative group outline-none ${isProcessing ? 'cursor-not-allowed' : 'cursor-pointer'}`}
      >
        <div className={`
            absolute inset-0 rounded-full blur-3xl transition-all duration-700 opacity-20 group-hover:opacity-40
            ${isProcessing
            ? 'bg-yellow-500 scale-100'
            : (isRunning ? 'bg-cyber-primary scale-125 animate-pulse' : 'bg-cyber-danger scale-75')
          }
        `}></div>

        <div className={`
            relative w-36 h-36 rounded-full border-[2px] flex items-center justify-center transition-all duration-500
            backdrop-blur-sm shadow-2xl
            ${isProcessing
            ? 'border-yellow-500/40 bg-black/40'
            : (isRunning
              ? 'border-cyber-primary/40 bg-black/40 shadow-[0_0_40px_rgba(0,242,255,0.2)]'
              : 'border-white/5 bg-white/5 hover:border-cyber-danger/30 hover:bg-white/10'
            )
          }
        `}>
          <div className={`
              absolute inset-2 rounded-full border border-dashed border-white/20 transition-all duration-[2000ms]
              ${isProcessing
              ? 'animate-spin border-yellow-500/50 opacity-100'
              : (isRunning ? 'rotate-180 scale-100 opacity-100' : 'rotate-0 scale-95 opacity-30')
            }
          `}></div>

          {isProcessing ? (
            <Loader2
              size={42}
              className="animate-spin text-yellow-400 drop-shadow-[0_0_10px_rgba(250,204,21,0.8)]"
            />
          ) : (
            <Power
              size={42}
              className={`transition-all duration-500 
                ${isRunning
                  ? 'text-cyber-primary drop-shadow-[0_0_15px_rgba(0,242,255,1)] scale-110'
                  : 'text-white/20 group-hover:text-cyber-danger group-hover:drop-shadow-[0_0_10px_rgba(255,0,85,0.5)]'
                }
                `}
            />
          )}
        </div>
      </button>
    </div>
  );
}