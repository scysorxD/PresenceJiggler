using System.Drawing;

namespace PresenceJiggler;

internal sealed class TrayApplicationContext : ApplicationContext
{
    private AppSettings _settings;
    private readonly NotifyIcon _notifyIcon;
    private readonly System.Windows.Forms.Timer _timer;
    private readonly ToolStripMenuItem _toggleItem;
    private readonly ToolStripMenuItem _settingsItem;
    private long _lastAttemptAt;
    private bool _settingsOpen;

    public TrayApplicationContext()
    {
        _settings = SettingsStore.Load();
        var menu = new ContextMenuStrip();
        _toggleItem = new ToolStripMenuItem();
        _toggleItem.Click += (_, _) => Toggle();
        _settingsItem = new ToolStripMenuItem("Configuración...");
        _settingsItem.Click += (_, _) => OpenSettings();

        menu.Items.Add(_toggleItem);
        menu.Items.Add(_settingsItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(new ToolStripMenuItem("Salir", null, (_, _) => ExitThread()));

        _notifyIcon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Visible = true,
            ContextMenuStrip = menu
        };
        _notifyIcon.DoubleClick += (_, _) => OpenSettings();

        _timer = new System.Windows.Forms.Timer { Interval = 1_000 };
        _timer.Tick += OnTick;
        _timer.Start();
        RefreshMenu();
    }

    private void OnTick(object? sender, EventArgs e)
    {
        if (!_settings.Enabled || _settingsOpen)
            return;

        if (!WindowsInput.TryGetIdleDuration(out var idle) ||
            idle < TimeSpan.FromMinutes(_settings.IdleMinutes))
            return;

        // Evita reintentos constantes si el escritorio está bloqueado o rechaza SendInput.
        long now = Environment.TickCount64;
        if (now - _lastAttemptAt < 30_000)
            return;

        _lastAttemptAt = now;
        WindowsInput.NudgeCursor();
    }

    private void Toggle()
    {
        var next = _settings.Clone();
        next.Enabled = !next.Enabled;
        try
        {
            SettingsStore.Save(next);
            _settings = next;
            RefreshMenu();
        }
        catch (Exception ex)
        {
            ShowError("No se pudo guardar la configuración.", ex);
        }
    }

    private void OpenSettings()
    {
        if (_settingsOpen)
            return;

        _settingsOpen = true;
        try
        {
            using var form = new SettingsForm(_settings);
            if (form.ShowDialog() != DialogResult.OK)
                return;

            AppSettings next = form.Result;
            StartupManager.SetEnabled(next.StartWithWindows);
            SettingsStore.Save(next);
            _settings = next;
            RefreshMenu();
        }
        catch (Exception ex)
        {
            ShowError("No se pudo aplicar la configuración.", ex);
        }
        finally
        {
            _settingsOpen = false;
        }
    }

    private void RefreshMenu()
    {
        _toggleItem.Text = _settings.Enabled ? "Desactivar" : "Activar";
        _toggleItem.Checked = _settings.Enabled;
        _notifyIcon.Text = _settings.Enabled
            ? $"Presence Jiggler - Activo ({_settings.IdleMinutes} min)"
            : "Presence Jiggler - Desactivado";
    }

    private static void ShowError(string message, Exception error) =>
        MessageBox.Show($"{message}\n\n{error.Message}", "Presence Jiggler",
            MessageBoxButtons.OK, MessageBoxIcon.Error);

    protected override void ExitThreadCore()
    {
        _timer.Stop();
        _timer.Dispose();
        _notifyIcon.Visible = false;
        _notifyIcon.ContextMenuStrip?.Dispose();
        _notifyIcon.Dispose();
        base.ExitThreadCore();
    }
}
