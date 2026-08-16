using System;

namespace mRemoteNGSpecs.Support
{
    /// <summary>
    /// Names the issues a test covers, in a form a machine can read.
    ///
    /// The pipeline needs to answer "which test guards issue N?" without a human grepping comments,
    /// and a failure report needs to say which issues just regressed. A doc comment cannot do
    /// either. Tests are organised by behaviour area rather than one-per-issue, so a single test
    /// routinely covers several.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public sealed class IssuesAttribute(params string[] ids) : Attribute
    {
        public string[] Ids { get; } = ids;
    }

    /// <summary>
    /// Marks coverage that exercises a code path but cannot prove the defect is gone — races and
    /// repaint timing, where a pass means "did not reproduce this time". Kept separate from
    /// <see cref="IssuesAttribute"/> so a stress run is never reported as regression proof.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class StressCoverageAttribute(params string[] ids) : Attribute
    {
        public string[] Ids { get; } = ids;
    }
}
