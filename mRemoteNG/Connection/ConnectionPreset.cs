using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;

namespace mRemoteNG.Connection
{
    [SupportedOSPlatform("windows")]
    public class ConnectionPreset
    {
        private static readonly HashSet<string> ExcludedConnectionPropertyNames = new(StringComparer.Ordinal)
        {
            nameof(AbstractConnectionRecord.ConstantID),
            nameof(AbstractConnectionRecord.Name),
            nameof(AbstractConnectionRecord.CredentialId)
        };

        private static readonly IReadOnlyList<PropertyInfo> ConfigurableConnectionPropertiesInternal =
            ConnectionPropertyReflector
                .GetSerializableProperties()
                .Where(descriptor => !ExcludedConnectionPropertyNames.Contains(descriptor.Name))
                .Select(descriptor => descriptor.PropertyInfo)
                .ToArray();

        private static readonly IReadOnlyList<PropertyInfo> ConfigurableInheritancePropertiesInternal =
            new ConnectionInfoInheritance(new ConnectionInfo(), true)
                .GetProperties()
                .ToArray();

        public static IReadOnlyList<PropertyInfo> ConfigurableConnectionProperties =>
            ConfigurableConnectionPropertiesInternal;

        public static IReadOnlyList<PropertyInfo> ConfigurableInheritanceProperties =>
            ConfigurableInheritancePropertiesInternal;

        public string Name { get; set; } = string.Empty;

        public ConnectionInfo ConnectionInfo { get; } = new();

        public ConnectionInfoInheritance Inheritance => ConnectionInfo.Inheritance;

        public ConnectionPreset()
        {
        }

        public ConnectionPreset(string name)
        {
            Name = name?.Trim() ?? string.Empty;
        }

        public static ConnectionPreset FromConnection(string name, ConnectionInfo sourceConnection)
        {
            if (sourceConnection == null)
                throw new ArgumentNullException(nameof(sourceConnection));

            ConnectionPreset preset = new(name);
            preset.CaptureFrom(sourceConnection);
            return preset;
        }

        public ConnectionPreset Clone()
        {
            ConnectionPreset clonedPreset = new(Name);
            clonedPreset.CaptureFrom(ConnectionInfo);
            return clonedPreset;
        }

        public void CaptureFrom(ConnectionInfo sourceConnection)
        {
            if (sourceConnection == null)
                throw new ArgumentNullException(nameof(sourceConnection));

            CopyPropertyValues(
                ConfigurableConnectionPropertiesInternal,
                sourceConnection,
                ConnectionInfo);

            CopyPropertyValues(
                ConfigurableInheritancePropertiesInternal,
                sourceConnection.Inheritance,
                Inheritance);
        }

        public void ApplyTo(ConnectionInfo targetConnection)
        {
            if (targetConnection == null)
                throw new ArgumentNullException(nameof(targetConnection));

            CopyPropertyValues(
                ConfigurableConnectionPropertiesInternal,
                ConnectionInfo,
                targetConnection);

            CopyPropertyValues(
                ConfigurableInheritancePropertiesInternal,
                Inheritance,
                targetConnection.Inheritance);
        }

        private static void CopyPropertyValues(
            IEnumerable<PropertyInfo> properties,
            object source,
            object target)
        {
            foreach (PropertyInfo property in properties)
            {
                if (!property.CanRead || !property.CanWrite)
                    continue;

                object? value = property.GetValue(source, null);
                property.SetValue(target, value, null);
            }
        }
    }
}
