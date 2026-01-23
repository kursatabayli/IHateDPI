use std::path::PathBuf;
use tauri::{AppHandle, Manager};

pub fn get_app_data_dir(app: &AppHandle) -> PathBuf {
    let path = app
        .path()
        .app_config_dir()
        .expect("AppData dizini çözülemedi");

    if !path.exists() {
        let _ = std::fs::create_dir_all(&path);
    }

    path
}

pub fn get_install_dir() -> PathBuf {
    std::env::current_exe()
        .map(|path| path.parent().unwrap_or(&path).to_path_buf())
        .unwrap_or_else(|_| PathBuf::from("."))
}
