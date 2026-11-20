using System.Drawing;

namespace PresenceJiggler;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        using var singleInstance = new Mutex(initiallyOwned: true,
            name: @"Local\PresenceJiggler_9A9E9C7F", createdNew: out bool firstInstance);

        if (!firstInstance)
        {
            MessageBox.Show("Presence Jiggler ya está ejecutándose. Revisá el área de notificaciones junto al reloj.",
                "Presence Jiggler", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        ApplicationConfiguration.Initialize();
        using var iconStream = typeof(Program).Assembly.GetManifestResourceStream("PresenceJiggler.AppIcon.ico")
            ?? throw new InvalidOperationException("No se encontró el ícono de Presence Jiggler.");
        using var icon = new Icon(iconStream, SystemInformation.SmallIconSize);
        using var context = new TrayApplicationContext(icon);
        Application.Run(context);
    }
}
