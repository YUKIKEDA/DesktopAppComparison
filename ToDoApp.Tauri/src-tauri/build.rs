use std::fs;
use std::path::Path;

fn rerun_if_changed_recursive(dir: &Path) {
    let Ok(entries) = fs::read_dir(dir) else {
        return;
    };
    for entry in entries.flatten() {
        let path = entry.path();
        println!("cargo:rerun-if-changed={}", path.display());
        if path.is_dir() {
            rerun_if_changed_recursive(&path);
        }
    }
}

fn main() {
    // Directory-level rerun-if-changed does not track file contents; watch every asset.
    println!("cargo:rerun-if-changed=../dist");
    println!("cargo:rerun-if-changed=../dist/index.html");
    println!("cargo:rerun-if-changed=tauri.conf.json");
    rerun_if_changed_recursive(Path::new("../dist"));

    let index = Path::new("../dist/index.html");
    match fs::read_to_string(index) {
        Ok(html) => {
            let marker = html
                .lines()
                .find(|l| l.contains("assets/index-"))
                .unwrap_or("<no asset line>");
            println!("cargo:warning=embedding frontend from ../dist; {marker}");
        }
        Err(e) => println!("cargo:warning=../dist/index.html missing: {e}"),
    }

    tauri_build::build()
}
