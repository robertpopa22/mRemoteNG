using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;

namespace mRemoteNG.Tools
{
    [SupportedOSPlatform("windows")]
    public class ExternalToolsTypeConverter : StringConverter
    {
        public static string[] ExternalTools
        {
            get
            {
                List<string> externalToolList = new()
                {
                    // Add a blank entry to signify that no external tool is selected
                    string.Empty
                };

                foreach (ExternalTool externalTool in App.Runtime.ExternalToolsService.ExternalTools)
                {
                    externalToolList.Add(externalTool.DisplayName);
                }

                return externalToolList.ToArray();
            }
        }

        public override StandardValuesCollection GetStandardValues([NotNull] ITypeDescriptorContext? context)
        {
            if (context == null)
            {
                throw new System.ArgumentNullException(nameof(context));
            }

            return new StandardValuesCollection(ExternalTools);
        }

        public override bool GetStandardValuesExclusive([NotNull] ITypeDescriptorContext? context)
        {
            if (context == null)
            {
                throw new System.ArgumentNullException(nameof(context));
            }

            return true;
        }

        public override bool GetStandardValuesSupported([NotNull] ITypeDescriptorContext? context)
        {
            if (context == null)
            {
                throw new System.ArgumentNullException(nameof(context));
            }

            return true;
        }
    }
}