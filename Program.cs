using System.Configuration;

namespace local_packages_gui
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Main view = new Main();
            Application.Run(view);
        }
    }
}