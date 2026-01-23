import { useState, useRef, useEffect, ReactNode } from "react";
import { ChevronDown, Check } from "lucide-react";

interface Option {
  value: string;
  label: string;
  icon?: ReactNode;
}

interface SelectProps {
  value: string | null;
  onChange: (value: string) => void;
  options: Option[];
  placeholder?: string;
  disabled?: boolean;
  className?: string;
}

export function Select({ value, onChange, options, placeholder = "Seçiniz...", disabled, className }: SelectProps) {
  const [isOpen, setIsOpen] = useState(false);
  const containerRef = useRef<HTMLDivElement>(null);

  const selectedOption = options.find(opt => opt.value === value);

  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (containerRef.current && !containerRef.current.contains(event.target as Node)) {
        setIsOpen(false);
      }
    };
    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, []);

  return (
    <div className={`relative ${className || ""}`} ref={containerRef}>
      <button
        onClick={() => !disabled && setIsOpen(!isOpen)}
        className={`
          w-full flex items-center justify-between px-4 py-3 rounded-lg border text-xs font-medium transition-all duration-300
          ${isOpen
            ? 'bg-black/40 border-purple-500/50 shadow-[0_0_15px_rgba(168,85,247,0.15)] text-white'
            : 'bg-black/20 border-white/10 text-white/70 hover:border-white/20 hover:text-white'
          }
          ${disabled ? 'opacity-50 cursor-not-allowed' : 'cursor-pointer'}
        `}
      >
        <div className="flex items-center gap-2 truncate">
          {selectedOption?.icon && (
            <span className="flex-shrink-0 opacity-100">
              {selectedOption.icon}
            </span>
          )}
          <span className={value ? "text-white" : "text-white/30"}>
            {selectedOption?.label || placeholder}
          </span>
        </div>

        <ChevronDown
          size={14}
          className={`flex-shrink-0 ml-2 text-white/30 transition-transform duration-300 ${isOpen ? 'rotate-180 text-purple-400' : ''}`}
        />
      </button>

      <div className={`
        absolute z-50 left-0 right-0 mt-2 bg-[#0a0a0f] border border-white/10 rounded-xl overflow-hidden shadow-2xl origin-top transition-all duration-200
        ${isOpen
          ? 'opacity-100 scale-100 translate-y-0 visible'
          : 'opacity-0 scale-95 -translate-y-2 invisible pointer-events-none'
        }
      `}>
        <div className="max-h-60 overflow-y-auto py-1 custom-scrollbar">
          {options.map((option) => {
            const isSelected = option.value === value;
            return (
              <div
                key={option.value}
                onClick={() => {
                  onChange(option.value);
                  setIsOpen(false);
                }}
                className={`
                  relative flex items-center justify-between px-4 py-2.5 text-xs cursor-pointer transition-colors
                  ${isSelected
                    ? 'bg-purple-500/10 text-purple-400'
                    : 'text-white/60 hover:bg-white/5 hover:text-white'
                  }
                `}
              >
                <div className="flex items-center gap-2">
                  {option.icon && (
                    <span className="flex-shrink-0">
                      {option.icon}
                    </span>
                  )}
                  <span className="font-medium tracking-wide">{option.label}</span>
                </div>

                {isSelected && (
                  <Check size={12} className="text-purple-400 flex-shrink-0 ml-2" />
                )}
              </div>
            );
          })}
        </div>
      </div>
    </div>
  );
}