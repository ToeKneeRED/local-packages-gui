using System.Diagnostics;

namespace local_packages_gui
{
    internal sealed class VirtualFlowPanel : Panel
    {
        private int itemHeight = 55;
        private int itemSpacing = 0;
        int folderNameWidth = 200;
        private int hoveredIndex = -1;
        private List<string> _items = new();
        private List<string> _itemDirs = new();
        private List<string> _allItems = new List<string>();
        private List<string> _allDirs = new List<string>();
        private Dictionary<string, string> _itemHomepages = new();
        internal static readonly string[] Separator = new string[] { "    " };

        public void SetItemHomepages(Dictionary<string, string> homepages)
        {
            _itemHomepages = homepages;
            UpdateScroll();
            Invalidate();
        }

        public void SetAllItems(List<string> items, List<string> dirs)
        {
            _allItems = [.. items];
            _allDirs = [.. dirs];
            _items = [.. items];
            _itemDirs = [.. dirs];

            UpdateScroll();
            Invalidate();
        }

        public VirtualFlowPanel()
        {
            DoubleBuffered = true;
            AutoScroll = false;
            UpdateScroll();
        }

        private void UpdateScroll()
        {
            {
                int totalHeight = _items.Count * (itemHeight + itemSpacing);
                AutoScrollMinSize = new Size(0, totalHeight);
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            int clickedIndex = (-(this.AutoScrollPosition.Y) + e.Y) / (itemHeight + itemSpacing);
            if (clickedIndex >= 0 && clickedIndex < _items.Count)
            {
                string homepage = _itemHomepages[_itemDirs[clickedIndex]];

                Console.WriteLine($"Clicked {_items[clickedIndex]}\t{homepage}");
                OpenHomepageInBrowser(homepage);
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            var scroll = this.AutoScrollPosition;
            int index = (e.Y - scroll.Y) / (itemHeight + itemSpacing);

            if (index >= 0 && index < _items.Count)
            {
                if (hoveredIndex != index)
                {
                    hoveredIndex = index;
                    Invalidate();
                }
            }
            else
            {
                if (hoveredIndex != -1)
                {
                    hoveredIndex = -1;
                    Invalidate();
                }
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);

            hoveredIndex = -1;
            this.Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var g = e.Graphics;
            var scroll = this.AutoScrollPosition;
            int panelWidth = this.ClientSize.Width;

            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            int firstVisibleIndex = Math.Max(0, (-scroll.Y) / (itemHeight + itemSpacing));
            int lastVisibleIndex = Math.Min(_items.Count - 1, firstVisibleIndex + (this.ClientSize.Height / (itemHeight + itemSpacing)) + 2);

            for (int i = firstVisibleIndex; i <= lastVisibleIndex; i++)
            {
                int y = scroll.Y + i * (itemHeight + itemSpacing);

                Rectangle rect = new Rectangle(10, y, panelWidth - 20, itemHeight);

                Color backColor = (i == hoveredIndex) ? ColorTranslator.FromHtml("#3a96dd") : ColorTranslator.FromHtml("#2d2d30");
                Color borderColor = ColorTranslator.FromHtml("#4aa4ef");

                using (Brush backgroundBrush = new SolidBrush(backColor))
                using (Pen borderPen = new Pen(borderColor, 1.5f))
                using (var path = RoundedRect(rect, 8))
                {
                    g.FillPath(backgroundBrush, path);
                    g.DrawPath(borderPen, path);
                }

                string[] parts = _items[i].Split(new string[] { "    " }, StringSplitOptions.None);
                string folderName = parts[0];
                string description = parts.Length > 1 ? parts[1] : "";

                Color folderTextColor = (i == hoveredIndex) ? ColorTranslator.FromHtml("#080808") : Color.White;
                Color descriptionTextColor = (i == hoveredIndex) ? ColorTranslator.FromHtml("#1c1c1c") : Color.LightGray;

                using (var folderFont = new Font("Arial", 11, FontStyle.Bold))
                using (var sf = new StringFormat { LineAlignment = StringAlignment.Center, Alignment = StringAlignment.Near })
                using (var textBrush = new SolidBrush(folderTextColor))
                {
                    Rectangle folderRect = new Rectangle(rect.Left + 10, rect.Top, folderNameWidth, rect.Height);
                    g.DrawString(folderName, folderFont, textBrush, folderRect, sf);
                }

                using (var descriptionFont = new Font("Calibri", 11, FontStyle.Regular))
                using (var textBrush = new SolidBrush(descriptionTextColor))
                {
                    int descriptionWidth = panelWidth - folderNameWidth - 25;
                    Rectangle descriptionRect = new Rectangle(rect.Left + folderNameWidth - 35, rect.Top, descriptionWidth, rect.Height);

                    using (var sf = new StringFormat { LineAlignment = StringAlignment.Center, Alignment = StringAlignment.Near, FormatFlags = StringFormatFlags.NoClip })
                    {
                        sf.Trimming = StringTrimming.Word;
                        sf.FormatFlags |= StringFormatFlags.LineLimit;
                        g.DrawString(description, descriptionFont, textBrush, descriptionRect, sf);
                    }
                }
            }
        }

        private System.Drawing.Drawing2D.GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            int diameter = radius * 2;
            var path = new System.Drawing.Drawing2D.GraphicsPath();

            path.StartFigure();
            path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();

            return path;
        }

        private void OpenHomepageInBrowser(string homepageUrl)
        {
            if (!string.IsNullOrEmpty(homepageUrl))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = homepageUrl,
                    UseShellExecute = true
                });
            }
        }

        public void ApplySearchFilter(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                _items = new List<string>(_allItems);
                _itemDirs = new List<string>(_allDirs);
            }
            else
            {
                _items = new List<string>();
                _itemDirs = new List<string>();

                for (int i = 0; i < _allItems.Count; i++)
                {
                    if (_allItems[i].IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        _items.Add(_allItems[i]);
                        _itemDirs.Add(_allDirs[i]);
                    }
                }
            }

            UpdateScroll();
            Invalidate();
        }
    }
}

