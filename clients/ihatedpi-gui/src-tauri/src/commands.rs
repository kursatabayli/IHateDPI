use crate::models::{AppConfig, AppError, EngineConfig, TrayLocales};
use crate::process_manager::{is_engine_running, EngineState};
use crate::utils::{get_app_data_dir, get_install_dir};
use std::fs;
use std::path::PathBuf;
use std::sync::Mutex;
use tauri::menu::{Menu, MenuItem};
use tauri::{AppHandle, State};

#[derive(serde::Serialize)]
pub struct ScriptItem {
    pub name: String,
    pub full_path: String,
}

#[tauri::command]
pub fn get_external_scripts(folder_name: String) -> Result<Vec<ScriptItem>, AppError> {
    let scripts_dir = get_install_dir().join("Engines").join(&folder_name);

    if !scripts_dir.exists() {
        return Ok(Vec::new());
    }

    let mut scripts = Vec::new();
    if let Ok(entries) = fs::read_dir(scripts_dir) {
        for entry in entries.flatten() {
            let path = entry.path();
            if path.is_file() {
                let extension = path
                    .extension()
                    .and_then(|s| s.to_str())
                    .unwrap_or("")
                    .to_lowercase();

                if extension == "cmd" || extension == "bat" {
                    let name = path.file_name().unwrap().to_string_lossy().to_string();
                    let lower_name = name.to_lowercase();
                    if !lower_name.starts_with("service") && !lower_name.contains("install") {
                        scripts.push(ScriptItem {
                            name,
                            full_path: path.to_string_lossy().to_string(),
                        });
                    }
                }
            }
        }
    }

    scripts.sort_by(|a, b| a.name.cmp(&b.name));
    Ok(scripts)
}

#[tauri::command]
pub fn check_engine_status(folder_name: String, exe_name: String) -> bool {
    let base = get_install_dir().join("Engines").join(&folder_name);
    if base.join("x86_64").join(&exe_name).exists() {
        return true;
    }
    if base.join(&exe_name).exists() {
        return true;
    }
    false
}

#[tauri::command]
pub fn get_app_config(app: AppHandle) -> Result<AppConfig, AppError> {
    let config_path = get_app_data_dir(&app).join("appConfig.json");

    if !config_path.exists() {
        return Ok(AppConfig::default());
    }

    let content = match fs::read_to_string(&config_path) {
        Ok(c) => c,
        Err(_) => return Ok(AppConfig::default()),
    };

    match serde_json::from_str::<AppConfig>(&content) {
        Ok(config) => Ok(config),
        Err(e) => {
            println!(
                "Config uyumsuzluğu tespit edildi, ayarlar sıfırlanıyor: {}",
                e
            );

            let default_config = AppConfig::default();

            if let Ok(json) = serde_json::to_string_pretty(&default_config) {
                let _ = fs::write(&config_path, json);
            }

            Ok(default_config)
        }
    }
}

#[tauri::command]
pub fn save_app_config(app: AppHandle, config: AppConfig) -> Result<(), AppError> {
    let config_path = get_app_data_dir(&app).join("appConfig.json");
    let json = serde_json::to_string_pretty(&config)
        .map_err(|e| AppError::Config(format!("JSON oluşturma hatası: {}", e)))?;

    fs::write(config_path, json)?;
    Ok(())
}

fn get_engine_config_path() -> PathBuf {
    get_install_dir()
        .join("Engines")
        .join("IHateDPI")
        .join("engineConfig.json")
}

#[tauri::command]
pub fn get_engine_config(_app: AppHandle) -> Result<EngineConfig, AppError> {
    let config_path = get_engine_config_path();

    if !config_path.exists() {
        return Ok(EngineConfig::default());
    }

    let content = fs::read_to_string(config_path)?;

    match serde_json::from_str::<EngineConfig>(&content) {
        Ok(config) => Ok(config),
        Err(e) => {
            println!("Engine Config JSON hatası (varsayılan yükleniyor): {}", e);

            Ok(EngineConfig::default())
        }
    }
}

#[tauri::command]
pub fn save_engine_config(_app: AppHandle, config: EngineConfig) -> Result<(), AppError> {
    let config_path = get_engine_config_path();

    if let Some(parent) = config_path.parent() {
        if !parent.exists() {
            fs::create_dir_all(parent)?;
        }
    }

    let json = serde_json::to_string_pretty(&config)
        .map_err(|e| AppError::Config(format!("JSON oluşturma hatası: {}", e)))?;

    fs::write(config_path, json)?;
    Ok(())
}

#[tauri::command]
pub fn update_tray_lang(
    app: AppHandle,
    state: State<Mutex<TrayLocales>>,
    engine_state: State<EngineState>,
    locales: TrayLocales,
) -> Result<(), AppError> {
    {
        let mut current_locales = state
            .lock()
            .map_err(|_| AppError::Unknown("Lock hatası".into()))?;
        *current_locales = locales.clone();
    }

    let tray = app
        .tray_by_id("main")
        .ok_or(AppError::NotFound("Tray icon bulunamadı".into()))?;

    let is_running = is_engine_running(engine_state);

    let toggle_text = if is_running {
        &locales.stop
    } else {
        &locales.start
    };

    let toggle_i = MenuItem::with_id(&app, "toggle", toggle_text, true, None::<&str>)
        .map_err(|e| AppError::Unknown(e.to_string()))?;

    let quit_i = MenuItem::with_id(&app, "quit", &locales.quit, true, None::<&str>)
        .map_err(|e| AppError::Unknown(e.to_string()))?;

    let menu = Menu::with_items(&app, &[&toggle_i, &quit_i])
        .map_err(|e| AppError::Unknown(e.to_string()))?;

    tray.set_menu(Some(menu))
        .map_err(|e| AppError::Unknown(e.to_string()))?;

    Ok(())
}
