using FluentAssertions;
using SynthesisAIAgents.Api.Tools;

namespace SynthesisAIAgents.Tests.Tools
{
    internal class TestTool : ITool
    {
        public string Name { get; }
        public TestTool(string name) => Name = name;
        public System.Threading.Tasks.Task<string> ExecuteAsync(string inputJson, System.Threading.CancellationToken ct) =>
            throw new NotImplementedException();
    }

    public class ToolFactoryTests
    {
        [Fact]
        public void GetTool_ReturnsRegisteredTool_ByExactName()
        {
            // Arrange
            var t1 = new TestTool("alpha");
            var t2 = new TestTool("beta");
            var factory = new ToolFactory(new ITool[] { t1, t2 });

            // Act
            var got = factory.GetTool("alpha");

            // Assert
            got.Should().BeSameAs(t1);
        }

        [Fact]
        public void GetTool_IsCaseInsensitive()
        {
            // Arrange
            var t = new TestTool("MyTool");
            var factory = new ToolFactory(new[] { t });

            // Act
            var got1 = factory.GetTool("mytool");
            var got2 = factory.GetTool("MYTOOL");

            // Assert
            got1.Should().BeSameAs(t);
            got2.Should().BeSameAs(t);
        }

        [Fact]
        public void GetTool_ReturnsNull_WhenToolNotFound()
        {
            // Arrange
            var factory = new ToolFactory(new ITool[] { new TestTool("one") });

            // Act
            var got = factory.GetTool("does-not-exist");

            // Assert
            got.Should().BeNull();
        }

        [Fact]
        public void ListToolNames_ReturnsAllRegisteredNames()
        {
            // Arrange
            var t1 = new TestTool("alpha");
            var t2 = new TestTool("beta");
            var factory = new ToolFactory(new[] { t1, t2 });

            // Act
            var names = factory.ListToolNames().ToArray();

            // Assert
            names.Should().BeEquivalentTo(new[] { "alpha", "beta" });
        }

        [Fact]
        public void Constructor_Throws_WhenDuplicateToolNames()
        {
            // Arrange
            var t1 = new TestTool("dup");
            var t2 = new TestTool("DUP"); // duplicate different-casing

            // Act
            Action act = () => new ToolFactory(new[] { t1, t2 });

            // Assert
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void EmptyToolList_ProducesEmptyNames()
        {
            // Arrange
            var factory = new ToolFactory(Array.Empty<ITool>());

            // Act
            var names = factory.ListToolNames();

            // Assert
            names.Should().BeEmpty();
        }
    }
}