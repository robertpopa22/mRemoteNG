using System;
using System.Runtime.Versioning;
using System.Windows.Forms;

namespace mRemoteNG.UI;

[SupportedOSPlatform("windows")]
public class ResponsiveForm : Form
{
    public ResponsiveForm()
    {
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoScaleDimensions = new System.Drawing.SizeF(96, 96);
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        if (Controls.Count == 0) return;
        bool options = this is Forms.FrmOptions;
        using (var font = Themes.InterfaceFont.Create(Properties.OptionsAppearancePage.Default.UIFontFamily,
            Properties.OptionsAppearancePage.Default.UIFontSize, (System.Drawing.FontStyle)Properties.OptionsAppearancePage.Default.UIFontStyle))
        using (options ? new App.Diagnostics.OptionsLoadSpan(App.Diagnostics.OptionsLoadStage.LoadFontApply) : null)
            Themes.InterfaceFont.Apply(this, font, layoutWhenUnchanged: false);
        using (options ? new App.Diagnostics.OptionsLoadSpan(App.Diagnostics.OptionsLoadStage.LoadAdaptive) : null)
            AdaptiveLayout.Prepare(this);
        if (FormBorderStyle is FormBorderStyle.FixedDialog or FormBorderStyle.FixedSingle or FormBorderStyle.FixedToolWindow)
            FormBorderStyle = FormBorderStyle.Sizable;
        using (options ? new App.Diagnostics.OptionsLoadSpan(App.Diagnostics.OptionsLoadStage.LoadFit) : null)
            AdaptiveLayout.FitWorkingArea(this);
    }

    protected override void OnDpiChanged(DpiChangedEventArgs e)
    {
        base.OnDpiChanged(e);
        using var font = Themes.InterfaceFont.Create(Properties.OptionsAppearancePage.Default.UIFontFamily,
            Properties.OptionsAppearancePage.Default.UIFontSize, (System.Drawing.FontStyle)Properties.OptionsAppearancePage.Default.UIFontStyle);
        Themes.InterfaceFont.Apply(this, font);
        AdaptiveLayout.FitWorkingArea(this);
    }
}
