using System;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Runtime.InteropServices;
using Microsoft.Win32;

public class CustomShell : Form
{
    private Button startMenuButton;
    private Panel taskbar;
    private ListBox appsMenuList;
    private NotifyIcon systemTray;
    private ContextMenuStrip trayMenu;
    private Panel quickSettingsPanel;
    private FlowLayoutPanel pinnedAppsPanel;

    [DllImport("user32.dll")]
    static extern bool LockWorkStation();

    public CustomShell()
    {
        // Form setup
        this.Text = "Custom Windows Shell";
        this.FormBorderStyle = FormBorderStyle.None;
        this.WindowState = FormWindowState.Maximized;
        this.BackColor = Color.FromArgb(229, 241, 251);

        // Taskbar
        taskbar = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 48,
            BackColor = Color.FromArgb(43, 43, 43)
        };

        // Start Menu Button
        startMenuButton = new Button
        {
            Text = "⊞", // Windows logo symbol
            Font = new Font("Segoe UI", 12),
            FlatStyle = FlatStyle.Flat,
            Width = 48,
            Height = 48,
            Location = new Point(0, 0),
            ForeColor = Color.White,
            BackColor = Color.Transparent
        };
        startMenuButton.Click += StartMenuButton_Click;

        // Pinned Apps Panel
        pinnedAppsPanel = new FlowLayoutPanel
        {
            Location = new Point(50, 0),
            Height = 48,
            AutoSize = true
        };

        // System Tray
        SetupSystemTray();

        taskbar.Controls.Add(startMenuButton);
        taskbar.Controls.Add(pinnedAppsPanel);
        this.Controls.Add(taskbar);

        // Apps Menu
        appsMenuList = new ListBox
        {
            Width = 300,
            Height = 400,
            Visible = false,
            Location = new Point(0, this.Height - 448),
            BackColor = Color.FromArgb(43, 43, 43),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.None
        };
        appsMenuList.SelectedIndexChanged += AppsMenuList_SelectedIndexChanged;

        // Quick Settings Panel
        SetupQuickSettings();

        this.Controls.Add(appsMenuList);
        PopulateTaskbarPins();
        PopulateAppsMenu();
    }

    private void SetupSystemTray()
    {
        // System Tray Panel
        Panel trayPanel = new Panel
        {
            Dock = DockStyle.Right,
            Width = 200,
            BackColor = Color.Transparent
        };

        // Clock
        Label clock = new Label
        {
            Text = DateTime.Now.ToString("HH:mm"),
            ForeColor = Color.White,
            Dock = DockStyle.Right,
            Width = 50,
            TextAlign = ContentAlignment.MiddleCenter
        };
        clock.Click += (s, e) => quickSettingsPanel.Visible = !quickSettingsPanel.Visible;

        // Timer for clock update
        Timer clockTimer = new Timer { Interval = 1000 };
        clockTimer.Tick += (s, e) => clock.Text = DateTime.Now.ToString("HH:mm");
        clockTimer.Start();

        // Tray icons
        Button wifi = new Button { Text = "WiFi", FlatStyle = FlatStyle.Flat, Width = 40, Height = 40, ForeColor = Color.White };
        Button volume = new Button { Text = "Vol", FlatStyle = FlatStyle.Flat, Width = 40, Height = 40, ForeColor = Color.White };
        Button power = new Button { Text = "Pwr", FlatStyle = FlatStyle.Flat, Width = 40, Height = 40, ForeColor = Color.White };

        trayPanel.Controls.Add(clock);
        trayPanel.Controls.Add(wifi);
        trayPanel.Controls.Add(volume);
        trayPanel.Controls.Add(power);
        wifi.Location = new Point(110, 4);
        volume.Location = new Point(150, 4);
        power.Location = new Point(190, 4);

        taskbar.Controls.Add(trayPanel);
    }

    private void SetupQuickSettings()
    {
        quickSettingsPanel = new Panel
        {
            Width = 300,
            Height = 400,
            Visible = false,
            Location = new Point(this.Width - 300, this.Height - 448),
            BackColor = Color.FromArgb(43, 43, 43),
            BorderStyle = BorderStyle.FixedSingle
        };

        // Quick Settings Buttons
        Button wifiToggle = new Button { Text = "Wi-Fi", Width = 80, Height = 80, ForeColor = Color.White };
        Button bluetoothToggle = new Button { Text = "Bluetooth", Width = 80, Height = 80, ForeColor = Color.White };
        Button hotspotToggle = new Button { Text = "Hotspot", Width = 80, Height = 80, ForeColor = Color.White };
        Button airplaneToggle = new Button { Text = "Airplane", Width = 80, Height = 80, ForeColor = Color.White };
        Button accessibilityToggle = new Button { Text = "Accessibility", Width = 80, Height = 80, ForeColor = Color.White };

        wifiToggle.Location = new Point(20, 20);
        bluetoothToggle.Location = new Point(110, 20);
        hotspotToggle.Location = new Point(200, 20);
        airplaneToggle.Location = new Point(20, 110);
        accessibilityToggle.Location = new Point(110, 110);

        // Power options
        Button lockBtn = new Button { Text = "Lock", Width = 80, ForeColor = Color.White };
        Button signOutBtn = new Button { Text = "Sign Out", Width = 80, ForeColor = Color.White };
        Button powerBtn = new Button { Text = "Power", Width = 80, ForeColor = Color.White };

        lockBtn.Location = new Point(20, 340);
        signOutBtn.Location = new Point(110, 340);
        powerBtn.Location = new Point(200, 340);

        lockBtn.Click += (s, e) => LockScreen();
        signOutBtn.Click += (s, e) => SignOut();
        powerBtn.Click += (s, e) => Shutdown();

        quickSettingsPanel.Controls.AddRange(new Control[] {
            wifiToggle, bluetoothToggle, hotspotToggle, airplaneToggle, accessibilityToggle,
            lockBtn, signOutBtn, powerBtn
        });

        this.Controls.Add(quickSettingsPanel);
    }

    private void PopulateTaskbarPins()
    {
        try
        {
            string pinnedPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                @"Microsoft\Internet Explorer\Quick Launch\User Pinned\TaskBar");
            
            if (Directory.Exists(pinnedPath))
            {
                var files = Directory.GetFiles(pinnedPath, "*.lnk");
                foreach (var file in files)
                {
                    Button pinButton = new Button
                    {
                        Text = Path.GetFileNameWithoutExtension(file),
                        FlatStyle = FlatStyle.Flat,
                        Width = 48,
                        Height = 48,
                        ForeColor = Color.White,
                        BackColor = Color.Transparent
                    };
                    pinButton.Click += (s, e) => Process.Start(file);
                    pinnedAppsPanel.Controls.Add(pinButton);
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading pinned apps: {ex.Message}");
        }
    }

    private void PopulateAppsMenu()
    {
        string[] directories = new string[]
        {
            @"C:\Program Files",
            @"C:\Program Files (x86)",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
                @"Microsoft\Windows\Start Menu\Programs"),
            @"C:\Users\Public\Desktop",
            Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory)
        };

        foreach (var dir in directories)
        {
            if (Directory.Exists(dir))
            {
                var executables = Directory.GetFiles(dir, "*.exe", SearchOption.AllDirectories)
                    .Concat(Directory.GetFiles(dir, "*.lnk", SearchOption.AllDirectories));
                
                foreach (var app in executables)
                {
                    appsMenuList.Items.Add(new AppItem
                    {
                        Name = Path.GetFileNameWithoutExtension(app),
                        Path = app
                    });
                }
            }
        }
    }

    private void StartMenuButton_Click(object sender, EventArgs e)
    {
        appsMenuList.Visible = !appsMenuList.Visible;
        quickSettingsPanel.Visible = false;
    }

    private void AppsMenuList_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (appsMenuList.SelectedItem is AppItem item)
        {
            try
            {
                Process.Start(item.Path);
                appsMenuList.Visible = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error launching {item.Name}: {ex.Message}");
            }
        }
    }

    private void LockScreen() => LockWorkStation();
    private void Shutdown() => Process.Start("shutdown", "/s /t 0");
    private void SignOut() => Process.Start("shutdown", "/l");

    private class AppItem
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public override string ToString() => Name;
    }

    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.Run(new CustomShell());
    }
}
