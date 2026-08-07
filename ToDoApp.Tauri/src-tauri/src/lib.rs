use std::fs;
use std::path::PathBuf;
use serde::{Deserialize, Serialize};
use tauri::{Manager, LogicalPosition, LogicalSize};

// Get data directory path
fn get_data_dir(app: &tauri::AppHandle) -> PathBuf {
    app.path()
        .app_data_dir()
        .expect("Failed to get app data directory")
}

#[derive(Debug, Serialize, Deserialize)]
struct WindowBounds {
    x: f64,
    y: f64,
    width: f64,
    height: f64,
}

#[tauri::command]
async fn get_app_data_dir(app: tauri::AppHandle) -> Result<String, String> {
    let data_dir = get_data_dir(&app);
    fs::create_dir_all(&data_dir).map_err(|e| e.to_string())?;
    Ok(data_dir.to_string_lossy().to_string())
}

fn restore_window_bounds(app: &tauri::AppHandle) {
    let window_file = get_data_dir(app).join("window.json");
    let Ok(content) = fs::read_to_string(&window_file) else {
        return;
    };
    let Ok(bounds) = serde_json::from_str::<WindowBounds>(&content) else {
        return;
    };
    let Some(window) = app.get_webview_window("main") else {
        return;
    };
    let _ = window.set_position(LogicalPosition::new(bounds.x, bounds.y));
    let _ = window.set_size(LogicalSize::new(
        bounds.width.max(800.0),
        bounds.height.max(600.0),
    ));
}

/// Best-effort real window opacity on Windows via Win32 layered attributes.
/// Returns true when applied.
#[tauri::command]
fn set_window_opacity(app: tauri::AppHandle, opacity: f64) -> Result<bool, String> {
    #[cfg(windows)]
    {
        use tauri::Manager;
        let window = app
            .get_webview_window("main")
            .ok_or_else(|| "main window not found".to_string())?;
        let hwnd = window.hwnd().map_err(|e| e.to_string())?;
        let alpha = (opacity.clamp(0.0, 1.0) * 255.0).round() as u8;

        // Win32 constants
        const GWL_EXSTYLE: i32 = -20;
        const WS_EX_LAYERED: isize = 0x00080000;
        const LWA_ALPHA: u32 = 0x00000002;

        #[link(name = "user32")]
        extern "system" {
            fn GetWindowLongW(hwnd: isize, index: i32) -> i32;
            fn SetWindowLongW(hwnd: isize, index: i32, new_long: i32) -> i32;
            fn SetLayeredWindowAttributes(
                hwnd: isize,
                chromakey: u32,
                alpha: u8,
                flags: u32,
            ) -> i32;
        }

        unsafe {
            let hwnd_isize = hwnd.0 as isize;
            let ex_style = GetWindowLongW(hwnd_isize, GWL_EXSTYLE);
            SetWindowLongW(hwnd_isize, GWL_EXSTYLE, ex_style | WS_EX_LAYERED as i32);
            let ok = SetLayeredWindowAttributes(hwnd_isize, 0, alpha, LWA_ALPHA);
            if ok == 0 {
                return Err("SetLayeredWindowAttributes failed".to_string());
            }
        }
        return Ok(true);
    }

    #[cfg(not(windows))]
    {
        let _ = (app, opacity);
        Ok(false)
    }
}

#[cfg_attr(mobile, tauri::mobile_entry_point)]
pub fn run() {
    tauri::Builder::default()
        .plugin(tauri_plugin_dialog::init())
        .plugin(tauri_plugin_fs::init())
        .plugin(tauri_plugin_opener::init())
        .invoke_handler(tauri::generate_handler![get_app_data_dir, set_window_opacity])
        .setup(|app| {
            let _ = fs::create_dir_all(get_data_dir(app.handle()));
            restore_window_bounds(app.handle());
            // Apply ~0.95 opacity on Windows when possible
            let _ = set_window_opacity(app.handle().clone(), 0.95);
            Ok(())
        })
        .run(tauri::generate_context!())
        .expect("error while running tauri application");
}
