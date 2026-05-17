import os
import subprocess
import time

import pytest
from pywinauto import Application


def test_main_window_exposes_core_controls():
    app_path = os.environ.get("PCVIEW_APP_PATH")
    if not app_path:
        raise AssertionError("PCVIEW_APP_PATH is not set")

    process = subprocess.Popen([app_path])
    try:
        time.sleep(2)
        if process.poll() is not None:
            raise AssertionError("PcView exited before the UI was available")

        if not _has_main_window(process.pid):
            pytest.skip("PcView did not expose a visible main window in this automation session")

        app = Application(backend="uia").connect(process=process.pid, timeout=15)
        window = app.window(title="PcView")
        window.wait("visible", timeout=15)

        assert window.child_window(auto_id="ScanButton").exists(timeout=5)
        assert window.child_window(auto_id="RefreshButton").exists(timeout=5)
        assert window.child_window(auto_id="SearchBox").exists(timeout=5)
        assert window.child_window(auto_id="OverviewPanel").exists(timeout=5)

        window.child_window(auto_id="AllFilter").click_input()
        assert window.child_window(auto_id="AppsGrid").exists(timeout=5)
    finally:
        process.kill()
        process.wait(timeout=10)


def _has_main_window(process_id: int) -> bool:
    try:
        import win32gui
        import win32process

        handles: list[int] = []

        def callback(hwnd, _):
            if not win32gui.IsWindowVisible(hwnd):
                return
            _, hwnd_process_id = win32process.GetWindowThreadProcessId(hwnd)
            if hwnd_process_id == process_id:
                handles.append(hwnd)

        win32gui.EnumWindows(callback, None)
        return bool(handles)
    except Exception:
        return True
