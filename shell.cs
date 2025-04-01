using System;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using System.Management;  // For WMI WiFi control
using NAudio.CoreAudioApi; // Add NAudio NuGet package for volume control

public class CustomShell : Form
{
    private Button startMenuButton;
    private Panel taskbar;
    private ListBox appsMenuList;
    private Panel quickSettingsPanel;
    private FlowLayoutPanel pinnedAppsPanel;
    private Button wifiButton;
    private Button volumeButton;

    [DllImport("user32.dll")]
    static extern bool LockWorkStation();

    public CustomShell()
    {
        this.Text = "Custom Windows Shell";
        this.FormBorderStyle = FormBorderStyle.None;
        this.WindowState = FormWindowState.Maximized;
        this.BackColor = Color.FromArgb(229, 241, 251);

        taskbar = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 48,
            BackColor = Color.FromArgb(43, 43, 43)
        };

        startMenuButton = new Button
        {
            Text = "⊞",
            Font = new Font("Segoe UI", 12),
            FlatStyle = FlatStyle.Flat,
            Width = 48,
            Height = 48,
            Location = new Point(0, 0),
            ForeColor = Color.White,
            BackColor = Color.Transparent
        };
        startMenuButton.Click += StartMenuButton_Click;

        pinnedAppsPanel = new FlowLayoutPanel
        {
            Location = new Point(50, 0),
            Height = 48,
            AutoSize = true
        };

        SetupSystemTray();

        taskbar.Controls.Add(startMenuButton);
        taskbar.Controls.Add(pinnedAppsPanel);
        this.Controls.Add(taskbar);

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

        SetupQuickSettings();

        this.Controls.Add(appsMenuList);
        PopulateTaskbarPins();
        PopulateAppsMenu();
    }

    private void SetupSystemTray()
    {
        Panel trayPanel = new Panel
        {
            Dock = DockStyle.Right,
            Width = 200,
            BackColor = Color.Transparent
        };

        Label clock = new Label
        {
            Text = DateTime.Now.ToString("HH:mm"),
            ForeColor = Color.White,
            Dock = DockStyle.Right,
            Width = 50,
            TextAlign = ContentAlignment.MiddleCenter
        };
        clock.Click += (s, e) => quickSettingsPanel.Visible = !quickSettingsPanel.Visible;

        Timer clockTimer = new Timer { Interval = 1000 };
        clockTimer.Tick += (s, e) => clock.Text = DateTime.Now.ToString("HH:mm");
        clockTimer.Start();

        wifiButton = new Button { Text = "WiFi", FlatStyle = FlatStyle.Flat, Width = 40, Height = 40, ForeColor = Color.White };
        volumeButton = new Button { Text = "Vol", FlatStyle = FlatStyle.Flat, Width = 40, Height = 40, ForeColor = Color.White };
        Button power = new Button { Text = "Pwr", FlatStyle = FlatStyle.Flat, Width = 40, Height = 40, ForeColor = Color.White };

        wifiButton.Click += ToggleWifi;
        volumeButton.Click += ToggleVolume;

        trayPanel.Controls.Add(clock);
        trayPanel.Controls.Add(wifiButton);
        trayPanel.Controls.Add(volumeButton);
        trayPanel.Controls.Add(power);
        wifiButton.Location = new Point(110, 4);
        volumeButton.Location = new Point(150, 4);
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

        Button wifiToggle = new Button { Text = "Wi-Fi", Width = 80, Height = 80, ForeColor = Color.White };
        Button bluetoothToggle = new Button { Text = "Bluetooth", Width = 80, Height = 80, ForeColor = Color.White };
        Button hotspotToggle = new Button { Text = "Hotspot", Width = 80, Height = 80, ForeColor = Color.White };
        Button airplaneToggle = new Button { Text = "Airplane", Width = 80, Height = 80, ForeColor = Color.White };
        Button accessibilityToggle = new Button { Text = "Accessibility", Width = 80, Height = 80, ForeColor = Color.White };

        wifiToggle.Click += ToggleWifi;
        // Bluetooth, Hotspot, Airplane Mode would need additional APIs

        wifiToggle.Location = new Point(20, 20);
        bluetoothToggle.Location = new Point(110, 20);
        hotspotToggle.Location = new Point(200, 20);
        airplaneToggle.Location = new Point(20, 110);
        accessibilityToggle.Location = new Point(110, 110);

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

    private void ToggleWifi(object sender, EventArgs e)
    {
        try
        {
            bool isEnabled = IsWifiEnabled();
            SetWifiEnabled(!isEnabled);
            wifiButton.Text = IsWifiEnabled() ? "WiFi On" : "WiFi Off";
            ((Button)quickSettingsPanel.Controls[0]).Text = IsWifiEnabled() ? "Wi-Fi On" : "Wi-Fi Off";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"WiFi toggle error: {ex.Message}");
        }
    }

    private void ToggleVolume(object sender, EventArgs e)
    {
        try
        {
            var enumerator = new MMDeviceEnumerator();
            var device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            float currentVolume = device.AudioEndpointVolume.MasterVolumeLevelScalar;
            device.AudioEndpointVolume.MasterVolumeLevelScalar = currentVolume < 0.5f ? 1.0f : 0.0f;
            volumeButton.Text = device.AudioEndpointVolume.MasterVolumeLevelScalar > 0 ? "Vol On" : "Vol Off";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Volume toggle error: {ex.Message}");
        }
    }

    private bool IsWifiEnabled()
    {
        using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_NetworkAdapter WHERE NetConnectionID = 'Wi-Fi'"))
        {
            foreach (ManagementObject obj in searcher.Get())
            {
                return (bool)obj["NetEnabled"];
            }
        }
        return false;
    }

    private void SetWifiEnabled(bool enable)
    {
        using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_NetworkAdapter WHERE NetConnectionID = 'Wi-Fi'"))
        {
            foreach (ManagementObject obj in searcher.Get())
            {
                if (enable)
                    obj.InvokeMethod("Enable", null);
                else
                    obj.InvokeMethod("Disable", null);
            }
        }
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
