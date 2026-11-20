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
        Application.Run(new TrayApplicationContext());
    }
}
