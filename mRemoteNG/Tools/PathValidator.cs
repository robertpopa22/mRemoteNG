using System;
using System.IO;
using System.Runtime.Versioning;

namespace mRemoteNG.Tools
{
    /// <summary>
    /// Provides path validation to prevent path traversal attacks
    /// </summary>
    [SupportedOSPlatform("windows")]
    public static class PathValidator
    {
        /// <summary>
        /// Validates that a file path does not contain path traversal sequences
        /// </summary>
        /// <param name="path">The path to validate</param>
        /// <returns>True if the path is safe, false otherwise</returns>
        public static bool IsValidPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            // Check for path traversal sequences
            if (path.Contains("../") || path.Contains("..\\"))
                return false;

            // Also check for encoded versions of path traversal
            if (path.Contains("%2e%2e") || path.Contains("%2E%2E"))
                return false;

            return true;
        }

        /// <summary>
        /// Validates a file path and throws an exception if invalid
        /// </summary>
        /// <param name="path">The path to validate</param>
        /// <param name="parameterName">The name of the parameter being validated</param>
        /// <exception cref="ArgumentException">Thrown when the path contains path traversal sequences</exception>
        public static void ValidatePathOrThrow(string path, string parameterName = "path")
        {
            if (!IsValidPath(path))
                throw new ArgumentException("Invalid file path: path traversal sequences are not allowed", parameterName);
        }
    }
}
