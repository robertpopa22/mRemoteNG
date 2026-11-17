using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.Versioning;
using System.Windows.Forms;
using mRemoteNG.App.Diagnostics;

namespace mRemoteNG.UI.Forms.OptionsPages
{
    [SupportedOSPlatform("windows")]
    internal sealed class OptionsPageDescriptor
    {
        private readonly Func<Bitmap?> _iconProvider;
        private readonly Func<OptionsPage> _factory;
        private Bitmap? _scaledIcon;

        public OptionsPageDescriptor(
            string pageKey,
            string pageName,
            Func<Bitmap?> iconProvider,
            Func<OptionsPage> factory)
        {
            ArgumentNullException.ThrowIfNull(pageKey);
            ArgumentNullException.ThrowIfNull(pageName);
            ArgumentNullException.ThrowIfNull(iconProvider);
            ArgumentNullException.ThrowIfNull(factory);

            PageKey = pageKey;
            PageName = pageName;
            _iconProvider = iconProvider;
            _factory = factory;
        }

        public string PageKey { get; }

        public string PageName { get; }

        public OptionsPage? Page { get; private set; }

        public bool IsBuilt => Page is { IsDisposed: false };

        public bool IsLoaded { get; private set; }

        public bool NeedsReload { get; private set; }

        public Bitmap GetScaledIcon(DisplayProperties display)
        {
            ArgumentNullException.ThrowIfNull(display);
            if (_scaledIcon != null)
            {
                return _scaledIcon;
            }

            Bitmap source = _iconProvider() ?? Properties.Resources.F1Help_16x;
            _scaledIcon = display.ScaleImage(source);
            return _scaledIcon;
        }

        public OptionsPage EnsureReady(Action<OptionsPage>? onFirstBuild = null)
        {
            if (!IsBuilt)
            {
                OptionsPage created;
                using (new OptionsLoadSpan(OptionsLoadStage.PageCreate, PageKey))
                {
                    created = _factory();
                    created.Dock = DockStyle.Fill;
                }

                Page = created;
                created.IsLoadingSettings = true;
                try
                {
                    using (new OptionsLoadSpan(OptionsLoadStage.PageLanguage, PageKey))
                    {
                        created.ApplyLanguage();
                    }

                    using (new OptionsLoadSpan(OptionsLoadStage.PageRegistry, PageKey))
                    {
                        created.LoadRegistrySettings();
                    }

                    using (new OptionsLoadSpan(OptionsLoadStage.PageSettings, PageKey))
                    {
                        created.LoadSettings();
                    }

                    onFirstBuild?.Invoke(created);
                    created.HasChanges = false;
                    IsLoaded = true;
                    NeedsReload = false;
                }
                finally
                {
                    created.IsLoadingSettings = false;
                }

                return created;
            }

            OptionsPage existing = Page!;
            if (NeedsReload)
            {
                existing.IsLoadingSettings = true;
                try
                {
                    using (new OptionsLoadSpan(OptionsLoadStage.PageSettings, PageKey))
                    {
                        existing.LoadSettings();
                    }

                    existing.HasChanges = false;
                    IsLoaded = true;
                    NeedsReload = false;
                }
                finally
                {
                    existing.IsLoadingSettings = false;
                }
            }

            return existing;
        }

        public void SaveIfBuiltAndLoaded()
        {
            if (IsBuilt && IsLoaded)
            {
                Page!.SaveSettings();
            }
        }

        public void RevertAndInvalidate(bool reloadImmediately)
        {
            if (!IsBuilt)
            {
                return;
            }

            OptionsPage page = Page!;
            page.IsLoadingSettings = true;
            try
            {
                using (new OptionsLoadSpan(OptionsLoadStage.PageRevert, PageKey))
                {
                    page.RevertSettings();
                }

                if (reloadImmediately)
                {
                    using (new OptionsLoadSpan(OptionsLoadStage.PageSettings, PageKey))
                    {
                        page.LoadSettings();
                    }

                    IsLoaded = true;
                    NeedsReload = false;
                }
                else
                {
                    IsLoaded = false;
                    NeedsReload = true;
                }

                page.HasChanges = false;
            }
            finally
            {
                page.IsLoadingSettings = false;
            }
        }

        public void MarkNeedsReload()
        {
            if (!IsBuilt)
            {
                return;
            }

            IsLoaded = false;
            NeedsReload = true;
            Page!.HasChanges = false;
        }

        public void DisposePage()
        {
            if (Page is { IsDisposed: false } page)
            {
                page.Dispose();
            }

            Page = null;
            _scaledIcon?.Dispose();
            _scaledIcon = null;
            IsLoaded = false;
            NeedsReload = false;
        }
    }

    [SupportedOSPlatform("windows")]
    internal sealed class OptionsPageLifecycle
    {
        private readonly List<OptionsPageDescriptor> _descriptors;

        public OptionsPageLifecycle(IEnumerable<OptionsPageDescriptor> descriptors)
        {
            ArgumentNullException.ThrowIfNull(descriptors);
            _descriptors = descriptors.ToList();
        }

        public IReadOnlyList<OptionsPageDescriptor> Descriptors => _descriptors;

        public OptionsPageDescriptor? ActiveDescriptor { get; private set; }

        public int BuiltCount => _descriptors.Count(d => d.IsBuilt);

        public OptionsPage Activate(OptionsPageDescriptor descriptor, Action<OptionsPage>? onFirstBuild = null)
        {
            ArgumentNullException.ThrowIfNull(descriptor);
            OptionsPage page = descriptor.EnsureReady(onFirstBuild);
            ActiveDescriptor = descriptor;
            return page;
        }

        public OptionsPageDescriptor? FindByPageName(string? pageName)
        {
            if (_descriptors.Count == 0)
            {
                return null;
            }

            if (!string.IsNullOrEmpty(pageName))
            {
                foreach (OptionsPageDescriptor descriptor in _descriptors)
                {
                    if (string.Equals(descriptor.PageName, pageName, StringComparison.Ordinal))
                    {
                        return descriptor;
                    }
                }
            }

            return _descriptors[0];
        }

        public bool HasUnsavedChanges()
        {
            foreach (OptionsPageDescriptor descriptor in _descriptors)
            {
                if (descriptor.IsBuilt && descriptor.Page!.HasChanges)
                {
                    return true;
                }
            }

            return false;
        }

        public void SaveBuiltAndLoaded(Action? persistSettings = null)
        {
            foreach (OptionsPageDescriptor descriptor in _descriptors)
            {
                descriptor.SaveIfBuiltAndLoaded();
            }

            persistSettings?.Invoke();
            ClearChangeFlags();
        }

        public void ReloadAll()
        {
            using var reload = new OptionsLoadSpan(OptionsLoadStage.Reload);
            foreach (OptionsPageDescriptor descriptor in _descriptors)
            {
                descriptor.RevertAndInvalidate(reloadImmediately: ReferenceEquals(descriptor, ActiveDescriptor));
            }
        }

        public void InvalidateInactiveBuiltPages()
        {
            foreach (OptionsPageDescriptor descriptor in _descriptors)
            {
                if (!ReferenceEquals(descriptor, ActiveDescriptor))
                {
                    descriptor.MarkNeedsReload();
                }
            }
        }

        public void ClearChangeFlags()
        {
            foreach (OptionsPageDescriptor descriptor in _descriptors)
            {
                if (descriptor.IsBuilt)
                {
                    descriptor.Page!.HasChanges = false;
                }
            }
        }

        public void DisposeAll()
        {
            foreach (OptionsPageDescriptor descriptor in _descriptors)
            {
                descriptor.DisposePage();
            }

            _descriptors.Clear();
            ActiveDescriptor = null;
        }
    }
}
