use crate::models::AppConfig;
use crate::utils::get_app_data_dir;
use std::fs;
use tauri::AppHandle;

pub fn init(app: &AppHandle) -> Result<(), Box<dyn std::error::Error>> {
    let config_dir = get_app_data_dir(app);

    if !config_dir.exists() {
        fs::create_dir_all(&config_dir)?;
    }

    let config_path = config_dir.join("appConfig.json");

    if !config_path.exists() {
        let mut new_config = AppConfig::default();

        let sys_lang = sys_locale::get_locale()
            .map(|l| l.replace('_', "-"))
            .unwrap_or("en-US".to_string());

        new_config.language = Some(sys_lang);

        let json = serde_json::to_string_pretty(&new_config)?;
        fs::write(&config_path, json)?;

        #[cfg(debug_assertions)]
        println!("Config oluşturuldu: {:?}", config_path);
    }

    Ok(())
}
