import React from "react";

interface InputProps extends React.InputHTMLAttributes<HTMLInputElement> {
  label?: string;
  containerClassName?: string;
}

export function Input({ label, containerClassName, className, ...props }: InputProps) {
  return (
    <div className={`flex flex-col group ${containerClassName || ""}`}>
      {label && (
        // Etiket rengi white/30'dan white/70'e çekildi
        <span className="text-[10px] font-bold tracking-wider text-white/70 uppercase mb-1.5 ml-1 group-focus-within:text-cyber-primary transition-colors duration-300">
          {label}
        </span>
      )}
      <input
        className={`
          w-full bg-black/40 border border-white/10 rounded-lg px-3 py-2.5 
          text-xs text-white placeholder-white/50 
          outline-none transition-all duration-300
          hover:border-white/20
          focus:border-cyber-primary/50 focus:bg-cyber-primary/5 focus:shadow-[0_0_15px_rgba(0,242,255,0.1)]
          disabled:opacity-40 disabled:cursor-not-allowed
          ${className || ""}
        `}
        {...props}
      />
    </div>
  );
}