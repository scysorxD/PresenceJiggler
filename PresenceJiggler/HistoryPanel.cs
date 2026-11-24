using System.ComponentModel;
using System.Globalization;

namespace PresenceJiggler;

internal sealed class HistoryPanel : UserControl
{
    private readonly ActivityHistoryStore _store;
    private readonly IContainer _components = new Container();
    private readonly Label _summary = new() { AutoSize = true };
    private readonly ListView _days = CreateList(("Día", 125), ("Cantidad", 72));
    private readonly ListView _hours = CreateList(("Hora", 86), ("Cantidad", 72));
    private readonly ListView _movements = CreateList(("Hora local", 157));
    private readonly Button _exportAll = new() { Text = "Exportar 30 días…", AutoSize = true };
    private readonly Button _exportDay = new() { Text = "Exportar día…", AutoSize = true };
    private ActivityHistory? _history;
    private ILookup<DateOnly, DateTimeOffset>? _byDay;

    public HistoryPanel(ActivityHistoryStore store, bool historyWriteFailed)
    {
        _store = store;
        Dock = DockStyle.Fill;
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            ColumnCount = 3,
            RowCount = 4
        };
        for (int column = 0; column < 3; column++)
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / 3));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        Controls.Add(layout);

        var overview = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 10)
        };
        overview.Controls.Add(_summary);
        if (historyWriteFailed)
        {
            overview.Controls.Add(new Label
            {
                Text = "No se pudieron guardar algunos movimientos de esta sesión.",
                AutoSize = true
            });
        }
        layout.Controls.Add(overview, 0, 0);
        layout.SetColumnSpan(overview, 3);

        AddList(layout, "Movimientos por día", _days, 0);
        AddList(layout, "Movimientos por hora", _hours, 1);
        AddList(layout, "Movimientos del día", _movements, 2);

        var buttons = new FlowLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false
        };
        buttons.Controls.Add(_exportAll);
        buttons.Controls.Add(_exportDay);
        layout.Controls.Add(buttons, 0, 3);
        layout.SetColumnSpan(buttons, 3);

        _days.SelectedIndexChanged += (_, _) => ShowSelectedDay();
        _exportAll.Click += (_, _) => Export(null);
        _exportDay.Click += (_, _) =>
        {
            if (SelectedDay is { } day)
                Export(day);
        };

        var timer = new System.Windows.Forms.Timer(_components) { Interval = 30_000 };
        timer.Tick += (_, _) =>
        {
            if (_history?.LastDay != ActivityHistoryStore.LocalDay(DateTimeOffset.Now))
                Reload();
        };
        VisibleChanged += (_, _) =>
        {
            timer.Enabled = Visible;
            if (Visible)
                Reload();
        };
    }

    private DateOnly? SelectedDay =>
        _days.SelectedItems.Count == 0 ? null : (DateOnly)_days.SelectedItems[0].Tag!;

    private void Reload()
    {
        var today = ActivityHistoryStore.LocalDay(DateTimeOffset.Now);
        var selectedDay = SelectedDay ?? today;
        _days.BeginUpdate();
        try
        {
            _byDay = null;
            _days.Items.Clear();
            _history = _store.Load(today);
            _byDay = _history.Movements.ToLookup(ActivityHistoryStore.LocalDay);
            _summary.Text = $"Hoy: {_byDay[today].Count():N0}   |   Últimos 30 días: {_history.Movements.Count:N0}";
            _exportAll.Enabled = true;
            if (selectedDay < _history.FirstDay || selectedDay > today)
                selectedDay = today;

            for (var day = today; day >= _history.FirstDay; day = day.AddDays(-1))
            {
                var item = new ListViewItem([day.ToString("d"), _byDay[day].Count().ToString("N0")]) { Tag = day };
                _days.Items.Add(item);
                if (day == selectedDay)
                    item.Selected = true;
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _history = null;
            _summary.Text = "No se pudo leer el historial.";
            _exportAll.Enabled = false;
        }
        finally
        {
            _days.EndUpdate();
        }

        ShowSelectedDay();
    }

    private void ShowSelectedDay()
    {
        _hours.Items.Clear();
        _movements.Items.Clear();
        _exportDay.Enabled = _byDay is not null && SelectedDay.HasValue;
        if (_byDay is null || SelectedDay is not { } date)
            return;

        var movements = _byDay[date];
        var counts = movements.ToLookup(time => time.Hour);
        _hours.Items.AddRange(Enumerable.Range(0, 24)
            .Select(hour => new ListViewItem([$"{hour:00}:00", counts[hour].Count().ToString("N0")]))
            .ToArray());
        _movements.Items.AddRange(movements.Reverse()
            .Select(time => new ListViewItem(time.ToString("HH:mm:ss zzz", CultureInfo.InvariantCulture)))
            .ToArray());
    }

    private void Export(DateOnly? day)
    {
        using var dialog = new SaveFileDialog
        {
            Title = "Exportar historial",
            Filter = "Archivo JSON (*.json)|*.json",
            DefaultExt = "json",
            FileName = day is { } date ? $"PresenceJiggler-{date:yyyy-MM-dd}.json" : "PresenceJiggler-ultimos-30-dias.json"
        };
        if (dialog.ShowDialog(FindForm()) != DialogResult.OK)
            return;

        try
        {
            var now = DateTimeOffset.Now;
            var history = _store.Load(ActivityHistoryStore.LocalDay(now));
            if (day < history.FirstDay || day > history.LastDay)
            {
                MessageBox.Show(this, "El día seleccionado quedó fuera de los últimos 30 días.",
                    "Historial", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Reload();
                return;
            }

            ActivityHistoryStore.Export(dialog.FileName, history, day, now);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(this, $"No se pudo exportar el historial.\n\n{ex.Message}",
                "Historial", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static ListView CreateList(params (string Caption, int Width)[] columns)
    {
        var list = new ListView
        {
            View = View.Details,
            Dock = DockStyle.Fill,
            FullRowSelect = true,
            MultiSelect = false,
            HideSelection = false,
            HeaderStyle = ColumnHeaderStyle.Nonclickable
        };
        foreach (var (caption, width) in columns)
            list.Columns.Add(caption, width);
        return list;
    }

    private static void AddList(TableLayoutPanel layout, string label, ListView list, int column)
    {
        layout.Controls.Add(new Label { Text = label, AutoSize = true }, column, 1);
        list.AccessibleName = label;
        layout.Controls.Add(list, column, 2);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _components.Dispose();
        base.Dispose(disposing);
    }
}
