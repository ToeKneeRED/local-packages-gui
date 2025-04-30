#if DEBUG
using System.Runtime.InteropServices;
#endif
using Button = System.Windows.Forms.Button;
using TextBox = System.Windows.Forms.TextBox;

namespace local_packages_gui
{
    public partial class Main : Form
    {
#if DEBUG
        [DllImport("kernel32.dll")]
        private static extern bool AllocConsole();
#endif

        public Main()
        {
            InitializeComponent();

#if DEBUG
            AllocConsole();
#endif

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 1,
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 1));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            Controls.Add(layout);

            var searchBox = new RichTextBox
            {
                Font = new Font("Arial", 10f, FontStyle.Regular),
                Dock = DockStyle.Top,
                BorderStyle = BorderStyle.None,
                ForeColor = Color.White,
                BackColor = ColorTranslator.FromHtml("#444a52"),
                Margin = new Padding(5),
                Multiline = true, 
                AcceptsTab = false,
                ScrollBars = RichTextBoxScrollBars.None,
                DetectUrls = false,
                RichTextShortcutsEnabled = false,
                AllowDrop = false,
                Height = 20
            };
            Controls.Add(searchBox);

            var panel = new VirtualFlowPanel
            {
                Dock = DockStyle.Fill,
                BackColor = ColorTranslator.FromHtml("#262c34")
            };

            layout.Controls.Add(panel, 0, 1);

            string packagesPath = GetPackagesPath();
            List<string> folderNames = [];
            List<string> dirNames = [];
            Dictionary<string, string> homepages = new();

            foreach (string firstLevel in Directory.GetDirectories(packagesPath))
            {
                foreach (string secondLevel in Directory.GetDirectories(firstLevel))
                {
                    string dirName = Path.GetFileName(secondLevel);

                    string description = "";
                    string homepage = "";
                    string xmakePath = Path.Combine(secondLevel, "xmake.lua");

                    if (File.Exists(xmakePath))
                    {
                        string[] lines = File.ReadAllLines(xmakePath);
                        foreach (string line in lines)
                        {
                            string trimmed = line.Trim();
                            if (trimmed.StartsWith("set_description(", StringComparison.Ordinal))
                            {
                                int firstQuote = trimmed.IndexOf('"');
                                int lastQuote = trimmed.LastIndexOf('"');

                                if (firstQuote >= 0 && lastQuote > firstQuote)
                                {
                                    description = trimmed.Substring(firstQuote + 1, lastQuote - firstQuote - 1);
                                }
                            }
                            if (trimmed.StartsWith("set_homepage(", StringComparison.Ordinal))
                            {
                                int firstQuote = trimmed.IndexOf('"');
                                int lastQuote = trimmed.LastIndexOf('"');

                                if (firstQuote >= 0 && lastQuote > firstQuote)
                                {
                                    homepage = trimmed.Substring(firstQuote + 1, lastQuote - firstQuote - 1);
                                }
                            }
                        }
                    }

                    string displayName = string.IsNullOrEmpty(description) ? dirName : $"{dirName}    {description}";

                    folderNames.Add(displayName);
                    dirNames.Add(dirName);
                    homepages.Add(dirName, homepage);
                }
            }

            panel.SetAllItems(folderNames, dirNames);
            panel.SetItemHomepages(homepages);

            searchBox.TextChanged += (s, e) =>
            {
                searchBox.SelectionStart = searchBox.Text.Length;
                searchBox.SelectionLength = 0;
                searchBox.SelectionFont = searchBox.Font;
                searchBox.SelectionColor = searchBox.ForeColor;

                if (searchBox.Text.Contains("\r") || searchBox.Text.Contains("\n"))
                {
                    searchBox.Text = searchBox.Text.Replace("\r", "").Replace("\n", "");
                    searchBox.SelectionStart = searchBox.Text.Length;
                }

                panel.ApplySearchFilter(searchBox.Text);
            };
            searchBox.KeyDown += (sender, e) =>
            {
                switch (e.KeyCode)
                {
                    case Keys.Enter:
                        e.SuppressKeyPress = true;
                        e.Handled = true;
                        return;
                    case Keys.Escape:
                        searchBox.Text = "";
                        searchBox.SelectionStart = 0;
                        return;
                    default:
                        switch (e)
                        {
                            case { Control: true, KeyCode: Keys.B or Keys.I or Keys.U }:
                                e.SuppressKeyPress = true;
                                e.Handled = true;
                                return;
                            case { Control: true, KeyCode: Keys.V }:
                            {
                                if (Clipboard.ContainsText())
                                {
                                    int start = searchBox.SelectionStart;
                                    string text = Clipboard.GetText(TextDataFormat.Text);
                                    searchBox.Text = searchBox.Text.Insert(start, text);
                                    searchBox.SelectionStart = start + text.Length;
                                }

                                e.SuppressKeyPress = true;
                                e.Handled = true;
                                break;
                            }
                        }

                        break;
                }
            };
        }

        private string GetPackagesPath()
        {
            string path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\.xmake";
            if (Directory.Exists(path))
            {
                Console.WriteLine($"Found local xmake: {path}");
                path += "\\repositories\\xmake-repo\\packages\\";
            }

            return path;
        }
    }
}