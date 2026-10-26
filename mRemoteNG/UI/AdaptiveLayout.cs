using System;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Windows.Forms;

namespace mRemoteNG.UI;

[SupportedOSPlatform("windows")]
internal static class AdaptiveLayout
{
    private static readonly ConditionalWeakTable<Control, object> Prepared = new();
    private static readonly ConditionalWeakTable<Control, object> PreparedInputs = new();

    internal static bool IsPreparedInput(Control control) => PreparedInputs.TryGetValue(control, out _);

    public static void Prepare(Control root)
    {
        if (Prepared.TryGetValue(root, out _)) return;
        Prepared.Add(root, new object());
        root.SuspendLayout();
        try
        {
            if (root is ContainerControl { AutoScaleMode: AutoScaleMode.Font } container)
            {
                container.PerformAutoScale();
                // The initial designer scale has already run. Future font changes
                // should remeasure rows, rather than rescale input minimum widths.
                container.AutoScaleMode = AutoScaleMode.Dpi;
                container.AutoScaleDimensions = new SizeF(root.DeviceDpi, root.DeviceDpi);
            }
            PrepareContainer(root);
            if (root is ScrollableControl scroll)
                scroll.AutoScroll = true;
        }
        finally { root.ResumeLayout(true); }
    }

    private static void PrepareContainer(Control parent)
    {
        parent.SuspendLayout();
        try { PrepareContainerCore(parent); }
        finally
        {
            if (parent is ScrollableControl scroll && parent is not ToolStripContainer)
                scroll.AutoScroll = parent is not TableLayoutPanel;
            parent.ResumeLayout(true);
        }
    }

    private static void PrepareContainerCore(Control parent)
    {
        var positions = parent.Controls.Cast<Control>().ToDictionary(c => c, c => c.Location);
        if (parent is SplitContainer split)
        {
            PrepareContainer(split.Panel1);
            PrepareContainer(split.Panel2);
            return;
        }
        if (parent is TabControl tabs)
        {
            foreach (TabPage page in tabs.TabPages) PrepareContainer(page);
            return;
        }
        if (parent is ToolStripContainer strips)
        {
            PrepareContainer(strips.ContentPanel);
            return;
        }
        if (parent is GroupBox group)
        {
            group.AutoSize = true;
            group.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        }
        if (parent is Panel panel && parent is not TableLayoutPanel && parent is not FlowLayoutPanel
            && (parent.Parent is TableLayoutPanel || parent.Dock is DockStyle.Top or DockStyle.Bottom) && !HasFlexibleContent(parent))
        {
            panel.AutoSize = true;
            panel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            if (panel.Dock == DockStyle.Fill) panel.Dock = DockStyle.Top;
        }
        if (parent is FlowLayoutPanel flow)
        {
            flow.WrapContents = true;
            flow.AutoScroll = true;
            flow.AutoSize = true;
            flow.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            foreach (ButtonBase button in flow.Controls.OfType<ButtonBase>())
            {
                button.AutoSize = true;
                if (button is Button pushButton) pushButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
                button.Dock = DockStyle.None;
                button.Anchor = AnchorStyles.None;
            }
            return;
        }
        Control[] children = parent.Controls.Cast<Control>().ToArray();
        foreach (Control child in children)
            if (!CollapsedCell(child) && child is TextBox { Multiline: false } or ComboBox or NumericUpDown)
            {
                if (!PreparedInputs.TryGetValue(child, out _))
                {
                    PreparedInputs.Add(child, new object());
                    int heightFloor = child.MinimumSize.Height;
                    child.FontChanged += (_, _) =>
                    {
                        child.MinimumSize = new Size(child.MinimumSize.Width, heightFloor);
                        child.Height = child.GetPreferredSize(Size.Empty).Height;
                    };
                    child.SizeChanged += (_, _) =>
                    {
                        if (child.MinimumSize.Height < child.Height)
                            child.MinimumSize = new Size(child.MinimumSize.Width, child.Height);
                    };
                }
                child.MinimumSize = new Size(Math.Max(child.MinimumSize.Width, (child is NumericUpDown ? 60 : 80) * parent.DeviceDpi / 96),
                    Math.Max(child.MinimumSize.Height, child.GetPreferredSize(Size.Empty).Height));
            }
        if (parent is TableLayoutPanel existingTable)
        {
            PrepareTable(existingTable);
            foreach (Control child in children)
                if (!CollapsedCell(child) && child is Panel or GroupBox or TabControl or SplitContainer or ToolStripContainer) PrepareContainer(child);
            if (!children.Any(HasFlexibleContent))
            {
                existingTable.AutoSize = true;
                existingTable.AutoSizeMode = AutoSizeMode.GrowAndShrink;
                if (existingTable.Dock == DockStyle.Fill || existingTable.Parent is GroupBox && existingTable.Dock is DockStyle.Left or DockStyle.Right)
                    existingTable.Dock = DockStyle.Top;
            }
            else
                FitFlexibleTable(existingTable);
            ConstrainTableWidth(existingTable);
            return;
        }
        foreach (Control child in children)
        {
            if (child is Panel or GroupBox or TabControl or SplitContainer or ToolStripContainer)
                PrepareContainer(child);
        }
        if (children.Length == 0) return;
        if (children.All(c => c.Dock == DockStyle.None))
        {
            if (parent is Form)
            {
                int lastTop = children.Max(c => positions[c].Y);
                Control[] footer = children.Where(c => Math.Abs(positions[c].Y - lastTop) <= 8 * parent.DeviceDpi / 96).ToArray();
                if (footer.All(c => c is Button))
                {
                    var body = new Panel { Name = "adaptiveBody", Dock = DockStyle.Fill, AutoScroll = true };
                    var actions = new Panel { Dock = DockStyle.Bottom };
                    parent.Controls.Clear();
                    body.Controls.AddRange(children.Except(footer).ToArray());
                    actions.Controls.AddRange(footer);
                    parent.Controls.Add(body);
                    parent.Controls.Add(actions);
                    PrepareContainer(body);
                    PrepareContainer(actions);
                    return;
                }
            }
            ReflowRows(parent, children, positions);
        }
    }

    private static bool Flexible(Control control) => control is ListBox or ListView or TreeView or PropertyGrid or DataGridView
        || control is TextBox { Multiline: true } or RichTextBox;

    private static bool HasFlexibleContent(Control control) => Flexible(control) || control.Controls.Cast<Control>().Any(HasFlexibleContent);

    private static bool CollapsedCell(Control control)
    {
        if (control.Parent is not TableLayoutPanel table) return false;
        int column = table.GetColumn(control);
        int row = table.GetRow(control);
        return column >= 0 && column < table.ColumnStyles.Count && table.GetColumnSpan(control) == 1
                && table.ColumnStyles[column].SizeType == SizeType.Absolute && table.ColumnStyles[column].Width == 0
            || row >= 0 && row < table.RowStyles.Count && table.GetRowSpan(control) == 1
                && table.RowStyles[row].SizeType == SizeType.Absolute && table.RowStyles[row].Height == 0;
    }

    private static void ReflowRows(Control parent, Control[] children, System.Collections.Generic.Dictionary<Control, Point> positions)
    {
        if (parent is Panel { Dock: DockStyle.Bottom } buttonPanel && children.All(c => c is Button))
        {
            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.RightToLeft, WrapContents = true, Padding = new Padding(6)
            };
            parent.Controls.Clear();
            foreach (Control child in children.OrderByDescending(c => c.Left))
            {
                child.AutoSize = true;
                buttons.Controls.Add(child);
            }
            buttonPanel.Controls.Add(buttons);
            buttonPanel.AutoSize = true;
            buttonPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            return;
        }
        var rows = children.OrderBy(c => positions[c].Y).ThenBy(c => positions[c].X)
            .Aggregate(new System.Collections.Generic.List<System.Collections.Generic.List<Control>>(), (result, child) =>
            {
                if (result.Count == 0 || Math.Abs(positions[result[^1][0]].Y - positions[child].Y) > 8 * parent.DeviceDpi / 96)
                    result.Add([child]);
                else result[^1].Add(child);
                return result;
            });
        int columns = rows.Max(row => row.Count);
        foreach (var row in rows) row.Sort((left, right) => positions[left].X.CompareTo(positions[right].X));
        var layout = new TableLayoutPanel
        {
            Name = "adaptiveContent", ColumnCount = columns, RowCount = rows.Count,
            Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(8), Margin = Padding.Empty
        };
        for (int column = 0; column < columns; column++)
        {
            bool inputColumn = rows.Any(row => row.Count > column && row[column] is TextBoxBase or ComboBox or NumericUpDown or Panel or GroupBox
                || row.Count > column && Flexible(row[column]));
            // Keep a usable share for captions even when a larger font changes their
            // preferred size during layout. An AutoSize caption beside a percent
            // input can otherwise collapse to a few pixels after font autoscaling.
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, inputColumn ? 2f : 1f));
        }
        float totalWeight = layout.ColumnStyles.Cast<ColumnStyle>().Sum(style => style.Width);
        int minimumWidth = (int)Math.Ceiling(Enumerable.Range(0, columns).Select(column =>
            rows.Where(row => row.Count > column && row.Count > 1)
                .Select(row => (row[column].MinimumSize.Width + 6) * totalWeight / layout.ColumnStyles[column].Width)
                .DefaultIfEmpty().Max()).Max()) + layout.Padding.Horizontal;
        minimumWidth = Math.Max(minimumWidth, rows.Where(row => row.Count == 1).Select(row => row[0].MinimumSize.Width + 6).DefaultIfEmpty().Max() + layout.Padding.Horizontal);
        layout.MinimumSize = new Size(minimumWidth, 0);
        bool hasFlexible = children.Any(Flexible);
        if (hasFlexible)
        {
            layout.AutoSize = false;
        }
        parent.Controls.Clear();
        for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            var row = rows[rowIndex].ToArray();
            layout.RowStyles.Add(new RowStyle(row.Any(Flexible) ? SizeType.Percent : SizeType.AutoSize, row.Any(Flexible) ? 100f : 0f));
            if (row.All(c => c is Button))
            {
                var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
                    FlowDirection = FlowDirection.RightToLeft, WrapContents = true };
                foreach (Control button in row.Reverse())
                {
                    button.AutoSize = true;
                    button.Anchor = AnchorStyles.None;
                    actions.Controls.Add(button);
                }
                layout.Controls.Add(actions, 0, rowIndex);
                layout.SetColumnSpan(actions, columns);
                continue;
            }
            for (int column = 0; column < row.Length; column++)
            {
                Control child = row[column];
                child.Margin = new Padding(3);
                child.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                if (Flexible(child))
                {
                    child.Dock = DockStyle.Fill;
                    child.MinimumSize = new Size(0, 100 * parent.DeviceDpi / 96);
                }
                else if (child is TextBoxBase or ComboBox or NumericUpDown)
                    child.Dock = DockStyle.Fill;
                else if (child is Button button)
                {
                    button.AutoSize = true;
                    button.AutoSizeMode = AutoSizeMode.GrowAndShrink;
                    button.Anchor = AnchorStyles.Top | AnchorStyles.Right;
                }
                else if (child is Label or CheckBox or RadioButton)
                    child.AutoSize = true;
                else if (child is GroupBox group)
                {
                    group.AutoSize = true;
                    group.AutoSizeMode = AutoSizeMode.GrowAndShrink;
                }
                else if (child is Panel contentPanel)
                {
                    contentPanel.AutoSize = true;
                    contentPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
                }
                else if (child is TableLayoutPanel nestedTable && !nestedTable.Controls.Cast<Control>().Any(Flexible))
                {
                    nestedTable.AutoSize = true;
                    nestedTable.AutoSizeMode = AutoSizeMode.GrowAndShrink;
                }
                layout.Controls.Add(child, column, rowIndex);
                if (row.Length == 1) layout.SetColumnSpan(child, columns);
            }
        }
        parent.Controls.Add(layout);
        if (parent is GroupBox)
            parent.MinimumSize = new Size(Math.Max(parent.MinimumSize.Width, layout.MinimumSize.Width + parent.Padding.Horizontal), parent.MinimumSize.Height);
        if (parent is Panel && parent.Parent is TableLayoutPanel tableParent)
        {
            int parentColumn = tableParent.GetColumn(parent);
            if (parentColumn >= 0 && parentColumn < tableParent.ColumnStyles.Count &&
                tableParent.ColumnStyles[parentColumn].SizeType == SizeType.AutoSize)
                parent.MinimumSize = new Size(Math.Max(parent.MinimumSize.Width, layout.MinimumSize.Width), parent.MinimumSize.Height);
        }
        if (parent is Panel panel && panel.Dock is DockStyle.Bottom or DockStyle.Top)
        {
            panel.AutoSize = true;
            panel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        }
        if (parent is ScrollableControl scroll)
        {
            scroll.AutoScroll = true;
            scroll.AutoScrollMinSize = new Size(Math.Max(scroll.AutoScrollMinSize.Width, layout.MinimumSize.Width), scroll.AutoScrollMinSize.Height);
        }
        PrepareTable(layout);
        if (hasFlexible)
        {
            void FitContent()
            {
                int height = layout.Padding.Vertical + rows.Sum(row => row.Max(child =>
                    (Flexible(child) ? child.MinimumSize.Height : child.GetPreferredSize(new Size(layout.ClientSize.Width, 0)).Height) + child.Margin.Vertical));
                int wanted = Math.Max(height, parent.ClientSize.Height);
                if (layout.Height != wanted) layout.Height = wanted;
            }
            layout.Layout += (_, _) => FitContent();
            parent.SizeChanged += (_, _) => FitContent();
            FitContent();
        }
        LimitTextWidth(layout);
    }

    private static void LimitTextWidth(TableLayoutPanel table)
    {
        int available = Math.Max(80, table.ClientSize.Width - table.Padding.Horizontal - 12);
        foreach (Control child in table.Controls)
            if (child is Label or CheckBox or RadioButton)
                child.MaximumSize = new Size(table.GetColumnSpan(child) == table.ColumnCount ? available : Math.Max(80, available / table.ColumnCount), 0);
    }

    private static void FitFlexibleTable(TableLayoutPanel table)
    {
        table.AutoSize = false;
        if (table.Dock is DockStyle.Fill or DockStyle.Left or DockStyle.Right) table.Dock = DockStyle.Top;
        void Fit()
        {
            int height = table.Padding.Vertical;
            for (int row = 0; row < table.RowCount; row++)
            {
                RowStyle? style = row < table.RowStyles.Count ? table.RowStyles[row] : null;
                height += style?.SizeType == SizeType.Absolute ? (int)Math.Ceiling(style.Height)
                    : style?.SizeType == SizeType.Percent ? 100 * table.DeviceDpi / 96
                    : table.Controls.Cast<Control>().Where(c => table.GetRow(c) == row && table.GetRowSpan(c) == 1)
                        .Select(c => c.GetPreferredSize(new Size(Math.Max(1, c.Width), 0)).Height + c.Margin.Vertical).DefaultIfEmpty().Max();
            }
            int available = table.Parent is { AutoSize: false } parent ? parent.ClientSize.Height : 0;
            int wanted = Math.Max(height, available);
            if (table.Height != wanted) table.Height = wanted;
        }
        table.Layout += (_, _) => Fit();
        if (table.Parent is { } parent) parent.SizeChanged += (_, _) => Fit();
        Fit();
    }

    private static void ConstrainTableWidth(TableLayoutPanel table)
    {
        if (table.Parent is not { } parent) return;
        void Fit()
        {
            int width = parent.DisplayRectangle.Width - table.Margin.Horizontal;
            if (parent is TableLayoutPanel outer)
            {
                int column = outer.GetColumn(table);
                if (column >= 0)
                    width = outer.GetColumnWidths().Skip(column).Take(outer.GetColumnSpan(table)).Sum() - table.Margin.Horizontal;
            }
            var maximum = new Size(Math.Max(table.MinimumSize.Width, Math.Max(32, width)), 0);
            if (table.MaximumSize != maximum) table.MaximumSize = maximum;
        }
        parent.Layout += (_, _) => Fit();
        Fit();
    }

    private static void PrepareTable(TableLayoutPanel table)
    {
        foreach (Button button in table.Controls.OfType<Button>())
        {
            button.AutoSize = true;
            button.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            button.Dock = DockStyle.None;
            button.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        }
        for (int column = 0; column < table.ColumnStyles.Count; column++)
            if (table.ColumnStyles[column].SizeType == SizeType.AutoSize &&
                table.Controls.Cast<Control>().Any(c => table.GetColumn(c) == column && table.GetColumnSpan(c) > 1 &&
                    c is TextBoxBase or ComboBox or NumericUpDown))
                table.ColumnStyles[column] = new ColumnStyle(SizeType.Percent, 100);
        if (table.ColumnCount == 2 && table.ColumnStyles.Count == 2 &&
            table.ColumnStyles.Cast<ColumnStyle>().All(c => c.SizeType == SizeType.AutoSize) &&
            table.Controls.Cast<Control>().Any(c => c is TextBoxBase or ComboBox or NumericUpDown))
        {
            for (int column = 0; column < 2; column++)
            {
                bool input = table.Controls.Cast<Control>().Any(c => table.GetColumn(c) == column && c is TextBoxBase or ComboBox or NumericUpDown);
                table.ColumnStyles[column] = new ColumnStyle(SizeType.Percent, input ? 2 : 1);
            }
        }
        for (int column = 0; column < table.ColumnStyles.Count; column++)
            if (table.ColumnStyles[column].SizeType == SizeType.Absolute && table.ColumnStyles[column].Width > 0 &&
                table.Controls.Cast<Control>().Any(c => table.GetColumn(c) == column && c is Label or Button or TextBoxBase or ComboBox))
                table.ColumnStyles[column].SizeType = SizeType.AutoSize;
        for (int row = 0; row < table.RowStyles.Count; row++)
            if (table.RowStyles[row].SizeType != SizeType.AutoSize && table.RowStyles[row].Height > 0 &&
                table.Controls.Cast<Control>().Any(c => table.GetRow(c) == row) &&
                !table.Controls.Cast<Control>().Any(c => table.GetRow(c) == row && HasFlexibleContent(c)))
                table.RowStyles[row].SizeType = SizeType.AutoSize;
        table.Layout += (_, _) => LimitCellTextWidth(table);
        LimitCellTextWidth(table);
        int[] inputRows = Enumerable.Range(0, table.RowStyles.Count)
            .Where(row => table.RowStyles[row].SizeType == SizeType.AutoSize &&
                table.Controls.Cast<Control>().Any(c => table.GetRow(c) == row && c is ComboBox)).ToArray();
        void MeasureInputRows()
        {
            foreach (int row in inputRows)
            {
                int height = table.Controls.Cast<Control>().Where(c => table.GetRow(c) == row && table.GetRowSpan(c) == 1)
                    .Select(c => Math.Max(c.GetPreferredSize(new Size(Math.Max(1, c.Width), 0)).Height,
                        c is ComboBox ? c.Height : 0) + c.Margin.Vertical).DefaultIfEmpty().Max();
                if (table.RowStyles[row].SizeType != SizeType.Absolute || Math.Abs(table.RowStyles[row].Height - height) > 0.5f)
                    table.RowStyles[row] = new RowStyle(SizeType.Absolute, height);
            }
        }
        table.Layout += (_, _) => MeasureInputRows();
        foreach (ComboBox combo in table.Controls.OfType<ComboBox>()) combo.SizeChanged += (_, _) => table.PerformLayout();
        MeasureInputRows();
    }

    private static void LimitCellTextWidth(TableLayoutPanel table)
    {
        int[] widths = table.GetColumnWidths();
        int available = Math.Max(32, table.ClientSize.Width - table.Padding.Horizontal);
        foreach (Control child in table.Controls)
        {
            if (child is not (Label or CheckBox or RadioButton or Button)) continue;
            if (CollapsedCell(child)) continue;
            if (child is Controls.MrngButton or Controls.MrngCheckBox or Controls.MrngRadioButton)
            {
                int glyph = child is Controls.MrngCheckBox or Controls.MrngRadioButton ? 16 * child.DeviceDpi / 96 : 0;
                int minimum = TextRenderer.MeasureText("W", child.Font).Width + glyph;
                if (child.MinimumSize.Width < minimum)
                    child.MinimumSize = new Size(minimum, child.MinimumSize.Height);
            }
            int column = table.GetColumn(child);
            int span = table.GetColumnSpan(child);
            if (column < 0 || column >= widths.Length) continue;
            int width = widths.Skip(column).Take(span).Sum();
            if (span == 1 && column < table.ColumnStyles.Count && table.ColumnStyles[column].SizeType == SizeType.AutoSize)
                width = available / Math.Min(3, table.ColumnCount);
            width = Math.Max(child.MinimumSize.Width, Math.Max(32, Math.Min(width, available) - child.Margin.Horizontal));
            var maximum = new Size(width, 0);
            if (child.MaximumSize != maximum) child.MaximumSize = maximum;
            child.AutoSize = true;
        }
    }

    public static void FitWorkingArea(Form form)
    {
        if (!form.TopLevel || form.WindowState != FormWindowState.Normal) return;
        Rectangle area = Screen.FromControl(form).WorkingArea;
        Panel? body = form.Controls.OfType<Panel>().FirstOrDefault(p => p.Name == "adaptiveBody");
        if (body is not null)
        {
            int wanted = body.Controls.Cast<Control>().Select(c => c.GetPreferredSize(new Size(body.ClientSize.Width, 0)).Height).DefaultIfEmpty().Max();
            if (wanted > body.ClientSize.Height)
                form.Height = Math.Min(area.Height, form.Height + wanted - body.ClientSize.Height);
        }
        else if (!HasFlexibleContent(form))
        {
            int wanted = form.Controls.Cast<Control>().Where(c => c.Dock is DockStyle.Top or DockStyle.None)
                .Select(c => c.Top + c.GetPreferredSize(new Size(form.ClientSize.Width, 0)).Height).DefaultIfEmpty().Max();
            if (wanted > form.ClientSize.Height)
                form.Height = Math.Min(area.Height, form.Height + wanted - form.ClientSize.Height);
        }
        form.MinimumSize = new Size(Math.Min(form.MinimumSize.Width, area.Width), Math.Min(form.MinimumSize.Height, area.Height));
        form.Size = new Size(Math.Min(form.Width, area.Width), Math.Min(form.Height, area.Height));
        form.Location = new Point(Math.Clamp(form.Left, area.Left, Math.Max(area.Left, area.Right - form.Width)),
            Math.Clamp(form.Top, area.Top, Math.Max(area.Top, area.Bottom - form.Height)));
    }
}
