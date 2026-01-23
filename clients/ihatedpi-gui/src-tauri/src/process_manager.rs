use crate::commands::get_app_config;
use crate::models::AppError;
use crate::utils::get_install_dir;
use std::fs::File;
use std::io::{BufRead, BufReader, Write};
use std::os::windows::process::CommandExt;
use std::path::PathBuf;
use std::process::{Child, Command, Stdio};
use std::sync::Mutex;
use tauri::{AppHandle, Emitter, State};

const CREATE_NO_WINDOW: u32 = 0x08000000;

pub struct EngineState {
    pub process: Mutex<Option<Child>>,
}

/// Script dosyasından (cmd/bat) argümanları okur
fn extract_args_from_file(file_path: &str, exe_path: &str) -> Vec<String> {
    let path = PathBuf::from(file_path);
    // EXE adını yoldan ayıkla (örn: "C:\...\goodbyedpi.exe" -> "goodbyedpi.exe")
    let exe_name = PathBuf::from(exe_path)
        .file_name()
        .unwrap_or_default()
        .to_string_lossy()
        .to_string();

    if let Ok(file) = File::open(path) {
        let reader = BufReader::new(file);
        for line in reader.lines() {
            if let Ok(l) = line {
                let clean = l.trim();
                // Yorum satırlarını ve boş satırları atla
                if clean.is_empty() || clean.starts_with("REM") || clean.starts_with("::") {
                    continue;
                }

                // Satır içinde exe adını ara (büyük/küçük harf duyarsız)
                // Örn: "goodbyedpi.exe -9 --dns-addr..." satırında "-9..." kısmını alır
                if let Some(idx) = clean.to_lowercase().find(&exe_name.to_lowercase()) {
                    let args_str = &clean[idx + exe_name.len()..];
                    return args_str.split_whitespace().map(|s| s.to_string()).collect();
                }
            }
        }
    }
    Vec::new()
}

#[tauri::command]
pub fn start_engine(app: AppHandle, state: State<EngineState>) -> Result<String, AppError> {
    let mut process_guard = state
        .process
        .lock()
        .map_err(|_| AppError::Unknown("Lock hatası".into()))?;

    if process_guard.is_some() {
        return Err(AppError::EngineRunning);
    }

    let config = get_app_config(app.clone())?;
    let child_process: Child;

    // AKTİF MOTOR SEÇİMİ
    if config.active_engine_id == "internal" {
        // --- 1. DAHİLİ MOTOR (IHateDPI) ---
        let base_dir = get_install_dir();
        let exe_path = base_dir
            .join("Engines")
            .join("IHateDPI")
            .join("IHateDPI Engine.exe");

        if !exe_path.exists() {
            return Err(AppError::NotFound("IHateDPI Engine.exe bulunamadı!".into()));
        }

        // Çalışma dizinini exe'nin olduğu klasör yapıyoruz.
        // base_dir zaten bir değişken olduğu için referansı geçerlidir.
        let work_dir = exe_path.parent().unwrap_or(&base_dir);

        child_process = Command::new(&exe_path)
            .current_dir(work_dir)
            .creation_flags(CREATE_NO_WINDOW)
            .stdin(Stdio::piped())
            .stdout(Stdio::null())
            .stderr(Stdio::null())
            .spawn()?;
    } else {
        // --- 2. HARİCİ MOTORLAR (Kullanıcı Tanımlı) ---

        let engine = config
            .external_engines
            .iter()
            .find(|e| e.id == config.active_engine_id)
            .ok_or(AppError::NotFound(
                "Seçili motor yapılandırması bulunamadı".into(),
            ))?;

        let exe_path = PathBuf::from(&engine.exe_path);
        if !exe_path.exists() {
            return Err(AppError::NotFound(format!(
                "EXE dosyası bulunamadı: {}",
                engine.exe_path
            )));
        }

        let mut args: Vec<String> = Vec::new();

        if let Some(script_path) = &engine.script_path {
            if !script_path.trim().is_empty() {
                args = extract_args_from_file(script_path, &engine.exe_path);
            }
        }

        if args.is_empty() {
            if let Some(manual_args) = &engine.manual_args {
                if !manual_args.trim().is_empty() {
                    args = manual_args
                        .split_whitespace()
                        .map(|s| s.to_string())
                        .collect();
                }
            }
        }

        // DÜZELTME BURADA YAPILDI:
        // &PathBuf::from(".") yerine std::path::Path::new(".") kullanıldı.
        // Bu sayede geçici değer hatası (temporary value dropped) oluşmaz.
        let work_dir = exe_path.parent().unwrap_or(std::path::Path::new("."));

        child_process = Command::new(&exe_path)
            .current_dir(work_dir)
            .args(args)
            .creation_flags(CREATE_NO_WINDOW)
            .spawn()?;
    }

    *process_guard = Some(child_process);

    let _ = app.emit("engine-status", true);

    Ok("Motor başlatıldı".to_string())
}

#[tauri::command]
pub fn stop_engine(app: AppHandle, state: State<EngineState>) -> Result<String, AppError> {
    let mut process_guard = state
        .process
        .lock()
        .map_err(|_| AppError::Unknown("Lock hatası".into()))?;

    if let Some(mut child) = process_guard.take() {
        let mut graceful_exit = false;

        // stdin varsa (IHateDPI gibi) "STOP" komutu dene
        if let Some(stdin) = child.stdin.as_mut() {
            if stdin.write_all(b"STOP\n").is_ok() {
                std::thread::sleep(std::time::Duration::from_secs(1));
                if let Ok(Some(_)) = child.try_wait() {
                    graceful_exit = true;
                }
            }
        }

        // Zorla kapat
        if !graceful_exit {
            let _ = child.kill();
        }

        let _ = app.emit("engine-status", false);

        return Ok("Motor durduruldu".to_string());
    }

    Err(AppError::Unknown("Çalışan motor yok".into()))
}

#[tauri::command]
pub fn is_engine_running(state: State<EngineState>) -> bool {
    let mut process_guard = match state.process.lock() {
        Ok(g) => g,
        Err(_) => return false,
    };

    if let Some(child) = process_guard.as_mut() {
        match child.try_wait() {
            Ok(Some(_)) => {
                *process_guard = None;
                false
            }
            Ok(None) => true,
            Err(_) => {
                *process_guard = None;
                false
            }
        }
    } else {
        false
    }
}