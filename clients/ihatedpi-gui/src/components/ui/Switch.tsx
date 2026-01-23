interface SwitchProps {
  label: string;
  checked: boolean;
  onChange: (checked: boolean) => void;
  disabled?: boolean;
}

export function Switch({ label, checked, onChange, disabled }: SwitchProps) {
  return (
    <div
      className={`flex items-center justify-between py-2 cursor-pointer group ${disabled ? 'opacity-40 pointer-events-none' : ''}`}
      onClick={() => !disabled && onChange(!checked)}
    >
      <span className="text-xs text-white/60 font-medium group-hover:text-white transition-colors">
        {label}
      </span>

      <div className={`
        relative w-9 h-5 rounded-full transition-all duration-300 border
        ${checked
          ? 'bg-cyber-primary/20 border-cyber-primary shadow-[0_0_10px_rgba(0,242,255,0.3)]'
          : 'bg-white/5 border-white/10 group-hover:border-white/20'}
      `}>
        <div className={`
          absolute top-0.5 left-0.5 w-3.5 h-3.5 rounded-full shadow-sm transform transition-transform duration-300
          ${checked ? 'translate-x-4 bg-cyber-primary' : 'translate-x-0 bg-white/40'}
        `}></div>
      </div>
    </div>
  );
}