#include <flutter/dart_project.h>
#include <flutter/flutter_view_controller.h>
#include <windows.h>

#include <cstdint>
#include <string>
#include <vector>

#include "flutter_window.h"
#include "utils.h"

namespace {

// FILETIME (100ns since 1601-01-01) to Unix epoch milliseconds.
uint64_t FileTimeToUnixMs(const FILETIME& ft) {
  ULARGE_INTEGER uli;
  uli.LowPart = ft.dwLowDateTime;
  uli.HighPart = ft.dwHighDateTime;
  constexpr uint64_t kEpochDiff = 116444736000000000ULL;
  return (uli.QuadPart - kEpochDiff) / 10000ULL;
}

uint64_t CurrentProcessStartUnixMs() {
  FILETIME creation{}, exit_time{}, kernel{}, user{};
  if (!::GetProcessTimes(::GetCurrentProcess(), &creation, &exit_time, &kernel,
                         &user)) {
    return 0;
  }
  return FileTimeToUnixMs(creation);
}

}  // namespace

int APIENTRY wWinMain(_In_ HINSTANCE instance, _In_opt_ HINSTANCE prev,
                      _In_ wchar_t *command_line, _In_ int show_command) {
  // Attach to console when present (e.g., 'flutter run') or create a
  // new console when running with a debugger.
  if (!::AttachConsole(ATTACH_PARENT_PROCESS) && ::IsDebuggerPresent()) {
    CreateAndAttachConsole();
  }

  // Initialize COM, so that it is available for use in the library and/or
  // plugins.
  ::CoInitializeEx(nullptr, COINIT_APARTMENTTHREADED);

  flutter::DartProject project(L"data");

  std::vector<std::string> command_line_arguments =
      GetCommandLineArguments();

  // OS process creation time for fair --ui-bench startup (includes engine boot).
  const uint64_t process_start_ms = CurrentProcessStartUnixMs();
  if (process_start_ms > 0) {
    command_line_arguments.insert(
        command_line_arguments.begin(),
        std::string("--process-start-ms=") + std::to_string(process_start_ms));
  }

  project.set_dart_entrypoint_arguments(std::move(command_line_arguments));

  FlutterWindow window(project);
  Win32Window::Point origin(10, 10);
  Win32Window::Size size(1280, 720);
  if (!window.Create(L"todoapp_flutter", origin, size)) {
    return EXIT_FAILURE;
  }
  window.SetQuitOnClose(true);

  ::MSG msg;
  while (::GetMessage(&msg, nullptr, 0, 0)) {
    ::TranslateMessage(&msg);
    ::DispatchMessage(&msg);
  }

  ::CoUninitialize();
  return EXIT_SUCCESS;
}
