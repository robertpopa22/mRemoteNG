using System;
using mRemoteNG.Tools;
using NUnit.Framework;

namespace mRemoteNGTests.Tools;

public class PathValidatorTests
{
    [Test]
    public void ValidPath_ReturnsTrue()
    {
        string validPath = @"C:\Users\TestUser\Documents\test.xml";
        Assert.That(PathValidator.IsValidPath(validPath), Is.True);
    }

    [Test]
    public void PathWithForwardSlashTraversal_ReturnsFalse()
    {
        string maliciousPath = @"C:\Users\TestUser\Documents\..\..\..\Windows\System32\test.xml";
        Assert.That(PathValidator.IsValidPath(maliciousPath), Is.False);
    }

    [Test]
    public void PathWithBackslashTraversal_ReturnsFalse()
    {
        string maliciousPath = @"C:\Users\TestUser\Documents\..\..\test.xml";
        Assert.That(PathValidator.IsValidPath(maliciousPath), Is.False);
    }

    [Test]
    public void PathWithMixedTraversal_ReturnsFalse()
    {
        string maliciousPath = @"C:\Users\TestUser\Documents\.././..\test.xml";
        Assert.That(PathValidator.IsValidPath(maliciousPath), Is.False);
    }

    [Test]
    public void PathWithEncodedTraversal_ReturnsFalse()
    {
        string maliciousPath = @"C:\Users\TestUser\Documents\%2e%2e\test.xml";
        Assert.That(PathValidator.IsValidPath(maliciousPath), Is.False);
    }

    [Test]
    public void PathWithUppercaseEncodedTraversal_ReturnsFalse()
    {
        string maliciousPath = @"C:\Users\TestUser\Documents\%2E%2E\test.xml";
        Assert.That(PathValidator.IsValidPath(maliciousPath), Is.False);
    }

    [Test]
    public void NullPath_ReturnsFalse()
    {
        Assert.That(PathValidator.IsValidPath(null), Is.False);
    }

    [Test]
    public void EmptyPath_ReturnsFalse()
    {
        Assert.That(PathValidator.IsValidPath(""), Is.False);
    }

    [Test]
    public void ValidatePathOrThrow_WithValidPath_DoesNotThrow()
    {
        string validPath = @"C:\Users\TestUser\Documents\test.xml";
        Assert.DoesNotThrow(() => PathValidator.ValidatePathOrThrow(validPath));
    }

    [Test]
    public void ValidatePathOrThrow_WithTraversalPath_ThrowsArgumentException()
    {
        string maliciousPath = @"C:\Users\TestUser\Documents\..\..\..\test.xml";
        var exception = Assert.Throws<ArgumentException>(() => PathValidator.ValidatePathOrThrow(maliciousPath));
        Assert.That(exception.Message, Does.Contain("path traversal"));
    }

    [Test]
    public void ValidatePathOrThrow_WithNullPath_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => PathValidator.ValidatePathOrThrow(null));
    }

    [Test]
    public void ValidatePathOrThrow_WithCustomParameterName_IncludesParameterName()
    {
        string maliciousPath = @"..\..\..\test.xml";
        var exception = Assert.Throws<ArgumentException>(() => PathValidator.ValidatePathOrThrow(maliciousPath, "customParam"));
        Assert.That(exception.ParamName, Is.EqualTo("customParam"));
    }
}
