using System.Drawing;

namespace PresenceJiggler;

internal sealed class SettingsForm : Form
{
    private readonly CheckBox _enabled;
    private readonly NumericUpDown _idleMinutes;
    private readonly CheckBox _startup;

    public AppSettings Result { get; private set; }

    public SettingsForm(AppSettings current, Icon icon)
    {
        Result = current.Clone();
        Text = "Presence Jiggler - Configuración";
        Icon = icon;
        Width = 435;
        Height = 275;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = true;
        Font = new Font("Segoe UI", 9F);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18, 15, 18, 15),
            ColumnCount = 2,
            RowCount = 5
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 43));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
        Controls.Add(layout);

        _enabled = new CheckBox
        {
            Text = "Activar movimiento automático",
            AutoSize = true,
            Checked = current.Enabled,
            Anchor = AnchorStyles.Left
        };
        layout.Controls.Add(_enabled, 0, 0);
        layout.SetColumnSpan(_enabled, 2);

        layout.Controls.Add(new Label
        {
            Text = "Minutos sin actividad antes de mover:",
            AutoSize = true,
            Anchor = AnchorStyles.Left
        }, 0, 1);

        _idleMinutes = new NumericUpDown
        {
            Minimum = 1,
            Maximum = 30,
            Value = Math.Clamp(current.IdleMinutes, 1, 30),
            Width = 75,
            Anchor = AnchorStyles.Left
        };
        layout.Controls.Add(_idleMinutes, 1, 1);

        _startup = new CheckBox
        {
            Text = "Iniciar con Windows (usuario actual)",
            AutoSize = true,
            Checked = StartupManager.IsEnabled(),
            Anchor = AnchorStyles.Left
        };
        layout.Controls.Add(_startup, 0, 2);
        layout.SetColumnSpan(_startup, 2);

        var note = new Label
        {
            Text = "Se ejecuta en segundo plano, solo en tu sesión de Windows. " +
                   "No modifica Teams ni garantiza su estado de presencia.",
            AutoSize = true,
            MaximumSize = new Size(355, 0),
            ForeColor = SystemColors.GrayText,
            Anchor = AnchorStyles.Top | AnchorStyles.Left
        };
        layout.Controls.Add(note, 0, 3);
        layout.SetColumnSpan(note, 2);

        var buttons = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            Dock = DockStyle.Fill,
            WrapContents = false
        };
        var save = new Button { Text = "Guardar", DialogResult = DialogResult.OK, Width = 95 };
        var cancel = new Button { Text = "Cancelar", DialogResult = DialogResult.Cancel, Width = 95 };
        buttons.Controls.Add(save);
        buttons.Controls.Add(cancel);
        layout.Controls.Add(buttons, 0, 4);
        layout.SetColumnSpan(buttons, 2);
        AcceptButton = save;
        CancelButton = cancel;

        save.Click += (_, _) =>
        {
            Result = new AppSettings
            {
                Enabled = _enabled.Checked,
                IdleMinutes = (int)_idleMinutes.Value,
                StartWithWindows = _startup.Checked
            };
        };
    }
}
