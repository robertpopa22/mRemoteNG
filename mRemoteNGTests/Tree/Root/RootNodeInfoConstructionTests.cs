using mRemoteNG.Tree.Root;
using NUnit.Framework;

namespace mRemoteNGTests.Tree.Root
{
    /// <summary>
    /// ContainerInfo's constructor runs SetDefaults, whose "New Folder" assignment
    /// virtual-dispatches into RootNodeInfo's Name setter and tramples the field initializer.
    /// Only the Guid-generating constructor used to repair the name afterwards; the
    /// (type, uniqueId) one -- which is how the SQL deserializer builds the tree root -- did not,
    /// so every SQL-loaded root was literally named "New Folder". (#148)
    /// </summary>
    public class RootNodeInfoConstructionTests
    {
        [Test]
        public void RootCreatedWithAnExplicitIdIsNotNamedNewFolder()
        {
            var root = new RootNodeInfo(RootNodeType.Connection, "0");

            Assert.That(root.Name, Does.Not.Contain("New Folder"));
        }

        [Test]
        public void BothConstructorsProduceTheSameDefaultName()
        {
            var byId = new RootNodeInfo(RootNodeType.Connection, "0");
            var byGuid = new RootNodeInfo(RootNodeType.Connection);

            Assert.That(byId.Name, Is.EqualTo(byGuid.Name));
        }

        [Test]
        public void TheRootTypeSurvivesConstruction()
        {
            var root = new RootNodeInfo(RootNodeType.PuttySessions, "0");

            Assert.That(root.Type, Is.EqualTo(RootNodeType.PuttySessions));
        }
    }
}
