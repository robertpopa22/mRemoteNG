using BrightIdeasSoftware;
using mRemoteNG.Connection;
using mRemoteNG.Tools;
using System.Runtime.Versioning;

namespace mRemoteNG.UI.Controls.ConnectionTree
{
    [SupportedOSPlatform("windows")]
    public class NameColumn : OLVColumn
    {
        public NameColumn(ImageGetterDelegate imageGetterDelegate)
        {
            AspectName = "Name";
            FillsFreeSpace = false;
            AspectGetter = item =>
            {
                var ci = (ConnectionInfo)item;
                return ConnectionNameFormatter.FormatName(ci);
            };
            ImageGetter = imageGetterDelegate;
            AutoCompleteEditor = false;
        }
    }
}