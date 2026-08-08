use std::fs;
use std::path::PathBuf;
use std::sync::Mutex;
use serde::{Deserialize, Serialize};
use tauri::{Emitter, Manager, LogicalPosition, LogicalSize, State};

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

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
struct CpuBenchConfig {
    enabled: bool,
    phase_file: Option<String>,
    json_path: Option<String>,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
struct UiBenchConfig {
    enabled: bool,
    out_file: Option<String>,
    json_path: Option<String>,
    process_start_ms: u64,
}

struct CpuBenchState(Mutex<CpuBenchConfig>);
struct UiBenchState(Mutex<UiBenchConfig>);

fn now_ms() -> u64 {
    std::time::SystemTime::now()
        .duration_since(std::time::UNIX_EPOCH)
        .unwrap_or_default()
        .as_millis() as u64
}

/// OS process creation time (Unix ms). Falls back to "now" if unavailable.
fn process_creation_ms() -> u64 {
    #[cfg(windows)]
    {
        use std::mem::MaybeUninit;
        #[repr(C)]
        struct FileTime {
            dw_low_date_time: u32,
            dw_high_date_time: u32,
        }
        #[link(name = "kernel32")]
        unsafe extern "system" {
            fn GetCurrentProcess() -> isize;
            fn GetProcessTimes(
                process: isize,
                creation: *mut FileTime,
                exit: *mut FileTime,
                kernel: *mut FileTime,
                user: *mut FileTime,
            ) -> i32;
        }
        unsafe {
            let mut creation = MaybeUninit::<FileTime>::uninit();
            let mut exit = MaybeUninit::<FileTime>::uninit();
            let mut kernel = MaybeUninit::<FileTime>::uninit();
            let mut user = MaybeUninit::<FileTime>::uninit();
            if GetProcessTimes(
                GetCurrentProcess(),
                creation.as_mut_ptr(),
                exit.as_mut_ptr(),
                kernel.as_mut_ptr(),
                user.as_mut_ptr(),
            ) != 0
            {
                let ft = creation.assume_init();
                let ticks =
                    ((ft.dw_high_date_time as u64) << 32) | (ft.dw_low_date_time as u64);
                // FILETIME → Unix ms
                const EPOCH_DIFF: u64 = 116444736000000000;
                if ticks > EPOCH_DIFF {
                    return (ticks - EPOCH_DIFF) / 10_000;
                }
            }
        }
    }
    now_ms()
}

fn parse_cpu_bench_args() -> CpuBenchConfig {
    let mut enabled = false;
    let mut phase_file: Option<String> = None;
    let mut json_path: Option<String> = None;

    for arg in std::env::args().skip(1) {
        if arg == "--cpu-bench" {
            enabled = true;
        } else if let Some(path) = arg.strip_prefix("--cpu-bench-phase=") {
            phase_file = Some(path.to_string());
        } else if !arg.starts_with('-') {
            let lower = arg.to_lowercase();
            if lower.ends_with(".json") && !lower.contains("package.json") {
                json_path = Some(arg);
            }
        }
    }

    CpuBenchConfig {
        enabled,
        phase_file,
        json_path,
    }
}

fn parse_ui_bench_args(process_start_ms: u64) -> UiBenchConfig {
    let mut enabled = false;
    let mut out_file: Option<String> = None;
    let mut json_path: Option<String> = None;

    for arg in std::env::args().skip(1) {
        if arg == "--ui-bench" {
            enabled = true;
        } else if let Some(path) = arg.strip_prefix("--ui-bench-out=") {
            out_file = Some(path.to_string());
        } else if !arg.starts_with('-') {
            let lower = arg.to_lowercase();
            if lower.ends_with(".json") && !lower.contains("package.json") {
                json_path = Some(arg);
            }
        }
    }

    UiBenchConfig {
        enabled,
        out_file,
        json_path,
        process_start_ms,
    }
}

#[tauri::command]
async fn get_app_data_dir(app: tauri::AppHandle) -> Result<String, String> {
    let data_dir = get_data_dir(&app);
    fs::create_dir_all(&data_dir).map_err(|e| e.to_string())?;
    Ok(data_dir.to_string_lossy().to_string())
}

#[tauri::command]
fn quit_app(app: tauri::AppHandle) {
    app.exit(0);
}

#[tauri::command]
fn get_cpu_bench_config(state: State<'_, CpuBenchState>) -> Result<CpuBenchConfig, String> {
    state
        .0
        .lock()
        .map(|g| g.clone())
        .map_err(|e| e.to_string())
}

#[tauri::command]
fn write_cpu_bench_phase(
    state: State<'_, CpuBenchState>,
    phase: String,
) -> Result<(), String> {
    let guard = state.0.lock().map_err(|e| e.to_string())?;
    if let Some(path) = &guard.phase_file {
        fs::write(path, format!("{phase}\n")).map_err(|e| e.to_string())?;
    }
    Ok(())
}

#[tauri::command]
fn get_ui_bench_config(state: State<'_, UiBenchState>) -> Result<UiBenchConfig, String> {
    state
        .0
        .lock()
        .map(|g| g.clone())
        .map_err(|e| e.to_string())
}

#[tauri::command]
fn write_ui_bench_result(
    state: State<'_, UiBenchState>,
    result: serde_json::Value,
) -> Result<(), String> {
    let guard = state.0.lock().map_err(|e| e.to_string())?;
    if let Some(path) = &guard.out_file {
        fs::write(path, result.to_string()).map_err(|e| e.to_string())?;
    }
    Ok(())
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

fn collect_json_paths_from_args() -> Vec<String> {
    std::env::args()
        .skip(1)
        .filter(|arg| {
            let lower = arg.to_lowercase();
            lower.ends_with(".json") && !lower.contains("package.json") && !arg.starts_with('-')
        })
        .collect()
}

fn emit_open_files(app: &tauri::AppHandle, paths: Vec<String>) {
    for path in paths {
        let _ = app.emit("open-file", path);
    }
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
    let process_start_ms = process_creation_ms();
    let cpu_bench = parse_cpu_bench_args();
    let ui_bench = parse_ui_bench_args(process_start_ms);
    // Always leave a breadcrumb for measure scripts when a phase file is provided.
    if let Some(path) = &cpu_bench.phase_file {
        let marker = if cpu_bench.enabled {
            "boot\n"
        } else {
            "disabled\n"
        };
        let _ = fs::write(path, marker);
    }

    tauri::Builder::default()
        .plugin(tauri_plugin_dialog::init())
        .plugin(tauri_plugin_fs::init())
        .plugin(tauri_plugin_opener::init())
        .plugin(tauri_plugin_notification::init())
        .plugin(tauri_plugin_clipboard_manager::init())
        .manage(CpuBenchState(Mutex::new(cpu_bench.clone())))
        .manage(UiBenchState(Mutex::new(ui_bench.clone())))
        .invoke_handler(tauri::generate_handler![
            get_app_data_dir,
            set_window_opacity,
            quit_app,
            get_cpu_bench_config,
            write_cpu_bench_phase,
            get_ui_bench_config,
            write_ui_bench_result
        ])
        .setup(move |app| {
            let _ = fs::create_dir_all(get_data_dir(app.handle()));
            restore_window_bounds(app.handle());
            // Apply ~0.95 opacity on Windows when possible
            let _ = set_window_opacity(app.handle().clone(), 0.95);

            if ui_bench.enabled {
                let req_path = get_data_dir(app.handle()).join("ui_bench_request.json");
                let req_body = serde_json::json!({
                    "enabled": true,
                    "outFile": ui_bench.out_file,
                    "jsonPath": ui_bench.json_path,
                    "processStartMs": ui_bench.process_start_ms,
                });
                let _ = fs::write(&req_path, req_body.to_string());

                let handle = app.handle().clone();
                let out_file = ui_bench.out_file.clone();
                let json_path = ui_bench.json_path.clone();
                let process_start_ms = ui_bench.process_start_ms;
                std::thread::spawn(move || {
                    for attempt in 0..6 {
                        std::thread::sleep(std::time::Duration::from_millis(if attempt == 0 {
                            800
                        } else {
                            700
                        }));
                        let _ = handle.emit(
                            "ui-bench-start",
                            serde_json::json!({
                                "outFile": out_file,
                                "jsonPath": json_path,
                                "processStartMs": process_start_ms,
                            }),
                        );
                        let _ = attempt;
                    }
                });
            } else if cpu_bench.enabled {
                if let Some(path) = &cpu_bench.phase_file {
                    let _ = fs::write(path, "boot\n");
                }
                // Sidecar request file — frontend polls this (more reliable than events alone).
                let req_path = get_data_dir(app.handle()).join("cpu_bench_request.json");
                let req_body = serde_json::json!({
                    "enabled": true,
                    "phaseFile": cpu_bench.phase_file,
                    "jsonPath": cpu_bench.json_path,
                });
                let _ = fs::write(&req_path, req_body.to_string());

                let handle = app.handle().clone();
                let phase_path = cpu_bench.phase_file.clone();
                let json_path = cpu_bench.json_path.clone();
                std::thread::spawn(move || {
                    // Emit only — do not overwrite the phase file (frontend owns idle/add/...).
                    for attempt in 0..6 {
                        std::thread::sleep(std::time::Duration::from_millis(if attempt == 0 {
                            800
                        } else {
                            700
                        }));
                        let _ = handle.emit(
                            "cpu-bench-start",
                            serde_json::json!({
                                "jsonPath": json_path,
                                "phaseFile": phase_path,
                            }),
                        );
                        let _ = attempt;
                    }
                });
            } else {
                let json_paths = collect_json_paths_from_args();
                if !json_paths.is_empty() {
                    let handle = app.handle().clone();
                    // Defer so the frontend listeners are ready
                    std::thread::spawn(move || {
                        std::thread::sleep(std::time::Duration::from_millis(800));
                        emit_open_files(&handle, json_paths);
                    });
                }
            }
            Ok(())
        })
        .build(tauri::generate_context!())
        .expect("error while building tauri application")
        .run(|app_handle, event| {
            #[cfg(any(target_os = "macos", target_os = "ios"))]
            if let tauri::RunEvent::Opened { urls } = &event {
                let paths: Vec<String> = urls
                    .iter()
                    .filter_map(|url| url.to_file_path().ok())
                    .filter(|p| {
                        p.extension()
                            .and_then(|e| e.to_str())
                            .map(|e| e.eq_ignore_ascii_case("json"))
                            .unwrap_or(false)
                    })
                    .map(|p| p.to_string_lossy().to_string())
                    .collect();
                if !paths.is_empty() {
                    emit_open_files(app_handle, paths);
                }
            }
            let _ = (app_handle, &event);
        });
}
