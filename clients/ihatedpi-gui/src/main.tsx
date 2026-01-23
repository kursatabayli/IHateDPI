import React from "react";
import ReactDOM from "react-dom/client";
import App from "./App";
import { setupI18n } from "./i18n";
import { ToastProvider } from "./context/ToastContext";

setupI18n().then(() => {
  ReactDOM.createRoot(document.getElementById("root") as HTMLElement).render(
    <React.StrictMode>
      <ToastProvider>
        <App />
      </ToastProvider>
    </React.StrictMode>
  );
});
