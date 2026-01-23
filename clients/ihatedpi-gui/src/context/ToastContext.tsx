import { createContext, useContext, useState, useCallback, ReactNode, useEffect } from "react";
import { X, CheckCircle, AlertTriangle, Info, AlertCircle } from "lucide-react";

type ToastType = "success" | "error" | "info" | "warning";

interface ToastData {
  id: string;
  message: string;
  type: ToastType;
}

interface ToastContextType {
  toast: {
    success: (msg: string) => void;
    error: (msg: string) => void;
    info: (msg: string) => void;
    warning: (msg: string) => void;
  };
}

const ToastContext = createContext<ToastContextType | undefined>(undefined);

export function ToastProvider({ children }: { children: ReactNode }) {
  const [toasts, setToasts] = useState<ToastData[]>([]);

  const addToast = useCallback((message: string, type: ToastType) => {
    const id = Math.random().toString(36).substring(2, 9);
    setToasts((prev) => [...prev, { id, message, type }]);
  }, []);

  const removeToast = useCallback((id: string) => {
    setToasts((prev) => prev.filter((t) => t.id !== id));
  }, []);

  const toast = {
    success: (msg: string) => addToast(msg, "success"),
    error: (msg: string) => addToast(msg, "error"),
    info: (msg: string) => addToast(msg, "info"),
    warning: (msg: string) => addToast(msg, "warning"),
  };

  return (
    <ToastContext.Provider value={{ toast }}>
      {children}
      <div className="fixed top-6 left-1/2 -translate-x-1/2 z-[100] flex flex-col items-center gap-3 pointer-events-none w-full max-w-md">
        {toasts.map((t) => (
          <ToastItem key={t.id} {...t} onRemove={() => removeToast(t.id)} />
        ))}
      </div>
    </ToastContext.Provider>
  );
}

function ToastItem({ message, type, onRemove }: ToastData & { onRemove: () => void }) {
  const [mounted, setMounted] = useState(false);
  const [isExiting, setIsExiting] = useState(false);

  useEffect(() => {
    requestAnimationFrame(() => {
      setMounted(true);
    });

    const timer = setTimeout(() => {
      setIsExiting(true);
    }, 3000);

    return () => clearTimeout(timer);
  }, []);

  useEffect(() => {
    if (isExiting) {
      const timer = setTimeout(() => {
        onRemove();
      }, 500);
      return () => clearTimeout(timer);
    }
  }, [isExiting, onRemove]);

  const baseClasses = "pointer-events-auto flex items-center gap-3 px-4 py-3 rounded-xl border backdrop-blur-md shadow-2xl min-w-[320px] max-w-[400px] transition-all duration-500 cubic-bezier(0.4, 0, 0.2, 1)";

  const typeClasses = {
    success: "bg-green-500/10 border-green-500/20 text-green-400 shadow-[0_0_15px_rgba(74,222,128,0.1)]",
    error: "bg-red-500/10 border-red-500/20 text-red-400 shadow-[0_0_15px_rgba(248,113,113,0.1)]",
    info: "bg-blue-500/10 border-blue-500/20 text-blue-400 shadow-[0_0_15px_rgba(96,165,250,0.1)]",
    warning: "bg-yellow-500/10 border-yellow-500/20 text-yellow-400 shadow-[0_0_15px_rgba(250,204,21,0.1)]"
  }[type];

  let animationClasses = "-translate-y-full opacity-0 scale-95";

  if (mounted && !isExiting) {
    animationClasses = "translate-y-0 opacity-100 scale-100";
  } else if (isExiting) {
    animationClasses = "-translate-y-[150%] opacity-0 scale-95";
  }

  return (
    <div className={`${baseClasses} ${typeClasses} ${animationClasses}`}>
      <div className="shrink-0">
        {type === "success" && <CheckCircle size={18} />}
        {type === "error" && <AlertCircle size={18} />}
        {type === "info" && <Info size={18} />}
        {type === "warning" && <AlertTriangle size={18} />}
      </div>

      <p className="text-xs font-medium text-white/90 flex-1 leading-relaxed drop-shadow-sm select-none">
        {message}
      </p>

      <button
        onClick={() => setIsExiting(true)}
        className="shrink-0 p-1 hover:bg-white/10 rounded-full transition-colors opacity-50 hover:opacity-100"
      >
        <X size={14} />
      </button>
    </div>
  );
}

export function useToast() {
  const context = useContext(ToastContext);
  if (!context) {
    throw new Error("useToast must be used within a ToastProvider");
  }
  return context.toast;
}