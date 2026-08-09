using System;
using System.IO;
using System.Windows.Forms;

namespace DeepSpaceChinese.ConfigEditor;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        string path = args.Length > 0 && !string.IsNullOrWhiteSpace(args[0])
            ? Path.GetFullPath(args[0])
            : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DeepSpaceChinese.ini");
        Application.Run(new ConfigEditorForm(path));
    }
}
