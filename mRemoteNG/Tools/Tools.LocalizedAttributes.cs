using System;
using System.ComponentModel;
using mRemoteNG.Resources.Language;

// ReSharper disable ArrangeAccessorOwnerBody

namespace mRemoteNG.Tools
{
    public class LocalizedAttributes
    {
        [AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = true)]
        public class LocalizedCategoryAttribute(string value, int Order = 1) : CategoryAttribute(value)
        {
            private const int MaxOrder = 10;
            private int Order = Order > MaxOrder ? MaxOrder : Order;

            protected override string GetLocalizedString(string value)
            {
                string OrderPrefix = "";
                for (int x = 0; x <= MaxOrder - Order; x++)
                {
                    OrderPrefix += Convert.ToString("\t");
                }

                return OrderPrefix + Language.ResourceManager.GetString(value);
            }
        }

        [AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = true)]
        public class LocalizedDisplayNameAttribute(string value) : DisplayNameAttribute(value)
        {
            private bool Localized = false;

            public override string DisplayName
            {
                get
                {
                    if (!Localized)
                    {
                        Localized = true;
                        DisplayNameValue = Language.ResourceManager.GetString(DisplayNameValue);
                    }

                    return base.DisplayName;
                }
            }
        }

        [AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = true)]
        public class LocalizedDescriptionAttribute(string value) : DescriptionAttribute(value)
        {
            private bool Localized = false;

            public override string Description
            {
                get
                {
                    if (!Localized)
                    {
                        Localized = true;
                        DescriptionValue = Language.ResourceManager.GetString(DescriptionValue);
                    }

                    return base.Description;
                }
            }
        }

        [AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = true)]
        public class LocalizedDefaultValueAttribute(string name) : DefaultValueAttribute(Language.ResourceManager.GetString(name))
        {

            // This allows localized attributes in a derived class to override a matching
            // non-localized attribute inherited from its base class
            public override object TypeId => typeof(DefaultValueAttribute);
        }

        #region Special localization - with String.Format

        [AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = true)]
        public class LocalizedDisplayNameInheritAttribute(string value) : DisplayNameAttribute(value)
        {
            private bool Localized = false;

            public override string DisplayName
            {
                get
                {
                    if (!Localized)
                    {
                        Localized = true;
                        DisplayNameValue = string.Format(Language.FormatInherit,
                                                         Language.ResourceManager.GetString(DisplayNameValue));
                    }

                    return base.DisplayName;
                }
            }
        }

        [AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = true)]
        public class LocalizedDescriptionInheritAttribute(string value) : DescriptionAttribute(value)
        {
            private bool Localized = false;

            public override string Description
            {
                get
                {
                    if (!Localized)
                    {
                        Localized = true;
                        DescriptionValue = string.Format(Language.FormatInheritDescription,
                                                         Language.ResourceManager.GetString(DescriptionValue));
                    }

                    return base.Description;
                }
            }
        }

        #endregion
    }
}