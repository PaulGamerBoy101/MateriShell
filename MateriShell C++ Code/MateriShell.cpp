#include <windows.h>
#include <winuser.h>
#include <vector>
#include <string>
#include <shellapi.h>

struct App {
    std::string name;
    std::string path;
};

std::vector<App> pinnedApps = {
    {"Notepad", "notepad.exe"},
    {"Calculator", "calc.exe"}
};

LRESULT CALLBACK WndProc(HWND hwnd, UINT msg, WPARAM wParam, LPARAM lParam) {
    switch (msg) {
        case WM_COMMAND:
            switch (LOWORD(wParam)) {
                case 1: MessageBox(hwnd, "About this shell", "About", MB_OK); break;
                case 2: MessageBox(hwnd, "Shell settings", "Settings", MB_OK); break;
                case 3: WinExec("cmd.exe", SW_SHOW); break;
                case 4: WinExec("taskmgr.exe", SW_SHOW); break;
                case 5: PostQuitMessage(0); break;
                default:
                    if (LOWORD(wParam) >= 1000) {
                        ShellExecute(NULL, "open", pinnedApps[LOWORD(wParam) - 1000].path.c_str(), NULL, NULL, SW_SHOWNORMAL);
                    }
                    break;
            }
            break;
        case WM_DESTROY:
            PostQuitMessage(0);
            break;
        default:
            return DefWindowProc(hwnd, msg, wParam, lParam);
    }
    return 0;
}

void CreateContextMenu(HWND hwnd) {
    HMENU hMenu = CreatePopupMenu();
    AppendMenu(hMenu, MF_STRING, 1, "About");
    AppendMenu(hMenu, MF_STRING, 2, "Shell Settings");
    AppendMenu(hMenu, MF_STRING, 3, "Run");
    AppendMenu(hMenu, MF_STRING, 4, "Task Manager");
    AppendMenu(hMenu, MF_STRING, 5, "Exit");

    POINT pt;
    GetCursorPos(&pt);
    TrackPopupMenu(hMenu, TPM_RIGHTBUTTON, pt.x, pt.y, 0, hwnd, NULL);
    DestroyMenu(hMenu);
}

int WINAPI WinMain(HINSTANCE hInstance, HINSTANCE hPrevInstance, LPSTR lpCmdLine, int nCmdShow) {
    WNDCLASS wc = {};
    wc.lpfnWndProc = WndProc;
    wc.hInstance = hInstance;
    wc.lpszClassName = "GnomeShellClone";
    wc.hbrBackground = CreateSolidBrush(RGB(245, 245, 245));
    RegisterClass(&wc);
    
    HWND hwnd = CreateWindowEx(
        0, "GnomeShellClone", "Custom Windows Shell", WS_OVERLAPPEDWINDOW,
        CW_USEDEFAULT, CW_USEDEFAULT, 1280, 720, NULL, NULL, hInstance, NULL);

    if (!hwnd) return 0;
    ShowWindow(hwnd, nCmdShow);
    UpdateWindow(hwnd);

    HWND hTopMenu = CreateWindowEx(0, "BUTTON", "🔍 Search  |  Programs  |  Places",
        WS_CHILD | WS_VISIBLE, 50, 0, 1180, 50, hwnd, (HMENU)100, hInstance, NULL);
    HWND hLogo = CreateWindowEx(0, "BUTTON", "☰",
        WS_CHILD | WS_VISIBLE, 0, 0, 50, 50, hwnd, (HMENU)200, hInstance, NULL);

    HWND hDock = CreateWindowEx(0, "BUTTON", "Dock (Taskbar Mode Available)",
        WS_CHILD | WS_VISIBLE, 0, 670, 1280, 50, hwnd, NULL, hInstance, NULL);

    HWND hStartMenu = CreateWindowEx(0, "STATIC", "Start Menu: Pinned Apps | Pinned Folders | Recents/Frequent",
        WS_CHILD | SS_CENTER, 10, 60, 400, 600, hwnd, NULL, hInstance, NULL);

    HWND hPlacesMenu = CreateWindowEx(0, "STATIC", "Places: User Folder, Documents, Downloads, Media, Recycle Bin",
        WS_CHILD | SS_CENTER, 420, 60, 400, 600, hwnd, NULL, hInstance, NULL);

    int buttonX = 10;
    for (size_t i = 0; i < pinnedApps.size(); i++) {
        CreateWindowEx(0, "BUTTON", pinnedApps[i].name.c_str(),
            WS_CHILD | WS_VISIBLE, buttonX, 670, 100, 50, hwnd, (HMENU)(1000 + i), hInstance, NULL);
        buttonX += 110;
    }

    MSG msg = {};
    while (GetMessage(&msg, NULL, 0, 0)) {
        if (msg.message == WM_LBUTTONDOWN && msg.hwnd == hLogo) {
            CreateContextMenu(hwnd);
        }
        TranslateMessage(&msg);
        DispatchMessage(&msg);
    }

    return (int)msg.wParam;
}
