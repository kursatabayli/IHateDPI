use crate::models::TrayLocales;
use crate::process_manager::{is_engine_running, start_engine, stop_engine, EngineState};
use std::sync::Mutex;
use tauri::{
    menu::{Menu, MenuItem},
    tray::{MouseButton, TrayIconBuilder, TrayIconEvent},
    App, Manager,
};

pub fn init(app: &mut App) -> Result<(), Box<dyn std::error::Error>> {
    // Varsayılan (İngilizce) ile başlat
    let default_locales = TrayLocales::default();

    let toggle_i = MenuItem::with_id(app, "toggle", &default_locales.start, true, None::<&str>)?;
    let quit_i = MenuItem::with_id(app, "quit", &default_locales.quit, true, None::<&str>)?;

    let menu = Menu::with_items(app, &[&toggle_i, &quit_i])?;

    let _ = TrayIconBuilder::with_id("main")
        .icon(app.default_window_icon().unwrap().clone())
        .menu(&menu)
        .show_menu_on_left_click(false)
        .on_menu_event(move |app, event| match event.id.as_ref() {
            "quit" => {
                app.exit(0);
            }
            "toggle" => {
                let state = app.state::<EngineState>();
                // Hafızadaki güncel dili çekiyoruz
                let locale_state = app.state::<Mutex<TrayLocales>>();
                let locales = locale_state.lock().unwrap();

                let running = is_engine_running(state.clone());
                let new_text;

                // Motor durumunu değiştir
                if running {
                    let _ = stop_engine(app.clone(), state.clone());
                    new_text = &locales.start; // Güncel dildeki "Başlat"
                } else {
                    let _ = start_engine(app.clone(), state.clone());
                    new_text = &locales.stop; // Güncel dildeki "Durdur"
                }

                // Menüyü güncelle
                if let Some(tray) = app.tray_by_id("main") {
                    if let Ok(toggle_item) =
                        MenuItem::with_id(app, "toggle", new_text, true, None::<&str>)
                    {
                        if let Ok(quit_item) =
                            MenuItem::with_id(app, "quit", &locales.quit, true, None::<&str>)
                        {
                            if let Ok(new_menu) = Menu::with_items(app, &[&toggle_item, &quit_item])
                            {
                                let _ = tray.set_menu(Some(new_menu));
                            }
                        }
                    }
                }
            }
            _ => {}
        })
        .on_tray_icon_event(|tray, event| {
            if let TrayIconEvent::Click {
                button: MouseButton::Left,
                ..
            } = event
            {
                let app = tray.app_handle();
                if let Some(window) = app.get_webview_window("main") {
                    let _ = window.show();
                    let _ = window.set_focus();
                }
            }
        })
        .build(app)?;

    Ok(())
}
