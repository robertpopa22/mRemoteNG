using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Design;
using System.Runtime.Versioning;
using mRemoteNG.Tools;

namespace mRemoteNG.UI.Controls.ConnectionInfoPropertyGrid
{
    /// <summary>
    /// Bridges the stock <see cref="ColorEditor"/> to the string-backed Color/TabColor properties.
    /// <para>
    /// Those properties store a colour as text (a named colour or <c>#RRGGBB</c>), but the stock
    /// editor is built for <see cref="Color"/>-typed properties: it always hands a
    /// <see cref="Color"/> back, and the PropertyGrid assigns an editor's return value straight
    /// through the PropertyDescriptor without consulting the declared TypeConverter. Assigning a
    /// Color to a string property is what produced "Object of type 'System.Drawing.Color' cannot
    /// be converted to type 'System.String'." (#176), and it is also why the swatch stayed blank:
    /// the stock editor paints nothing for a value that is not a Color.
    /// </para>
    /// <para>
    /// Conversion in both directions is delegated to <see cref="MiscTools.TabColorConverter"/>, so
    /// this editor introduces no second opinion about what a stored colour string means.
    /// </para>
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class TabColorEditor : UITypeEditor
    {
        private static readonly ColorEditor InnerEditor = new();
        private static readonly MiscTools.TabColorConverter Converter = new();

        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext? context) =>
            InnerEditor.GetEditStyle(context);

        public override object? EditValue(ITypeDescriptorContext? context, IServiceProvider provider, object? value)
        {
            Color initial = ToColor(context, value);
            object? picked = InnerEditor.EditValue(context, provider, initial);

            // The stock editor has no "cancelled" signal: dismissing the picker hands back the very
            // colour it was given. Treating that as a choice rewrote the stored text on every
            // Escape - "crimson" became "Crimson", "#FF0000" became "Red", and a legacy value the
            // converter could not parse (fed in as Color.Empty) came back as an empty string and
            // was lost. Only a colour that differs from what went in is a choice; everything else
            // leaves the stored value exactly as it was, unparseable text included (#176, PR #156).
            return picked is Color color && color != initial
                ? Converter.ConvertFrom(context, null, color)
                : value;
        }

        public override bool GetPaintValueSupported(ITypeDescriptorContext? context) => true;

        public override void PaintValue(PaintValueEventArgs e)
        {
            Color color = ToColor(e.Context, e.Value);

            // "No colour" is an empty string, and it must stay visibly empty — painting
            // Color.Empty would fill the swatch black and claim a colour that is not set.
            if (color.IsEmpty)
                return;

            InnerEditor.PaintValue(new PaintValueEventArgs(e.Context, color, e.Graphics, e.Bounds));
        }

        private static Color ToColor(ITypeDescriptorContext? context, object? value)
        {
            if (value is Color alreadyAColor)
                return alreadyAColor;

            try
            {
                return Converter.ConvertTo(context, null, value, typeof(Color)) as Color? ?? Color.Empty;
            }
            catch (Exception ex) when (ex is NotSupportedException or FormatException or ArgumentException)
            {
                // An unparsable stored value is treated as "no colour" rather than breaking the grid.
                return Color.Empty;
            }
        }
    }
}
