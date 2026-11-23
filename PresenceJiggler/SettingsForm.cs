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
        Height = 355;
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
            RowCount = 7
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 43));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
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

        var allDay = new CheckBox
        {
            Text = "Todo el día",
            AutoSize = true,
            Checked = current.AllDay,
            Anchor = AnchorStyles.Left
        };
        layout.Controls.Add(allDay, 0, 3);
        layout.SetColumnSpan(allDay, 2);

        var activeHours = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            Enabled = !current.AllDay,
            WrapContents = false
        };
        var activeStart = CreateTimePicker(current.ActiveStart);
        var activeEnd = CreateTimePicker(current.ActiveEnd);
        activeHours.Controls.Add(new Label { Text = "Desde", AutoSize = true, Margin = new Padding(0, 7, 6, 0) });
        activeHours.Controls.Add(activeStart);
        activeHours.Controls.Add(new Label { Text = "Hasta", AutoSize = true, Margin = new Padding(12, 7, 6, 0) });
        activeHours.Controls.Add(activeEnd);
        layout.Controls.Add(activeHours, 0, 4);
        layout.SetColumnSpan(activeHours, 2);
        allDay.CheckedChanged += (_, _) => activeHours.Enabled = !allDay.Checked;

        var note = new Label
        {
            Text = "Se ejecuta en segundo plano, solo en tu sesión de Windows. " +
                   "No modifica Teams ni garantiza su estado de presencia.",
            AutoSize = true,
            MaximumSize = new Size(355, 0),
            ForeColor = SystemColors.GrayText,
            Anchor = AnchorStyles.Top | AnchorStyles.Left
        };
        layout.Controls.Add(note, 0, 5);
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
        layout.Controls.Add(buttons, 0, 6);
        layout.SetColumnSpan(buttons, 2);
        AcceptButton = save;
        CancelButton = cancel;

        save.Click += (_, _) =>
        {
            var start = new TimeOnly(activeStart.Value.Hour, activeStart.Value.Minute);
            var end = new TimeOnly(activeEnd.Value.Hour, activeEnd.Value.Minute);
            if (!allDay.Checked && start == end)
            {
                DialogResult = DialogResult.None;
                MessageBox.Show(this, "Elegí horas distintas para el inicio y el fin del horario activo.",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                activeEnd.Focus();
                return;
            }

            Result = new AppSettings
            {
                Enabled = _enabled.Checked,
                IdleMinutes = (int)_idleMinutes.Value,
                StartWithWindows = _startup.Checked,
                AllDay = allDay.Checked,
                ActiveStart = start,
                ActiveEnd = end
            };
        };
    }

    private static DateTimePicker CreateTimePicker(TimeOnly time) => new()
    {
        Format = DateTimePickerFormat.Custom,
        CustomFormat = "HH:mm",
        ShowUpDown = true,
        Value = DateOnly.FromDateTime(DateTime.Today).ToDateTime(time),
        Width = 80
    };
}
