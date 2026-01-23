#![cfg_attr(
    all(not(debug_assertions), target_os = "windows"),
    windows_subsystem = "windows"
)]

mod commands;
mod config_init;
mod models;
mod process_manager;
mod system_tray;
mod utils;

use crate::models::TrayLocales;
use commands::*;
use process_manager::{is_engine_running, start_engine, stop_engine, EngineState};
use std::sync::Mutex;

fn main() {
    tauri::Builder::default()
        .plugin(tauri_plugin_dialog::init())
        .setup(|app| {
            if let Err(e) = config_init::init(app.handle()) {
                eprintln!("Config başlatılamadı: {}", e);
            }

            system_tray::init(app)?;

            Ok(())
        })
        .manage(Mutex::new(TrayLocales::default()))
        .manage(EngineState {
            process: Mutex::new(None),
        })
        .invoke_handler(tauri::generate_handler![
            get_external_scripts,
            get_engine_config,
            save_engine_config,
            check_engine_status,
            get_app_config,
            save_app_config,
            start_engine,
            stop_engine,
            is_engine_running,
            update_tray_lang,
        ])
        .run(tauri::generate_context!())
        .expect("error while running tauri application");
}
