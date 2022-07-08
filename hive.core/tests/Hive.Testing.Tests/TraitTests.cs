using FluentAssertions;
using Xunit;
using Xunit.Sdk;

namespace Hive.Testing.Tests
{
    public class TraitTests
    {
        [Fact]
        [UnitTest]
        public void UnitTestAttributeTests()
        {
            var method = typeof(TestData.Traits).GetMethod(nameof(TestData.Traits.UnitTest));

            var traits = TraitHelper.GetTraits(method!);

            traits.Should().Contain((kvp) =>

                kvp.Key == nameof(UnitTestAttribute.Category) && kvp.Value == UnitTestAttribute.Category
            );
        }

        [Fact]
        [UnitTest]
        public void IntegrationTestAttributeTests()
        {
            var method = typeof(TestData.Traits).GetMethod(nameof(TestData.Traits.IntegrationTest));

            var traits = TraitHelper.GetTraits(method!);

            traits.Should().Contain((kvp) =>

                kvp.Key == nameof(IntegrationTestAttribute.Category) && kvp.Value == IntegrationTestAttribute.Category
            );
        }

        [Fact]
        [UnitTest]
        public void ModuleTestAttributeTests()
        {
            var method = typeof(TestData.Traits).GetMethod(nameof(TestData.Traits.ModuleTest));

            var traits = TraitHelper.GetTraits(method!);

            traits.Should().Contain((kvp) =>

                kvp.Key == nameof(ModuleTestAttribute.Category) && kvp.Value == ModuleTestAttribute.Category
            );
        }

        [Fact]
        [UnitTest]
        public void SystemTestAttributeTests()
        {
            var method = typeof(TestData.Traits).GetMethod(nameof(TestData.Traits.SystemTest));

            var traits = TraitHelper.GetTraits(method!);

            traits.Should().Contain((kvp) =>

                kvp.Key == nameof(SystemTestAttribute.Category) && kvp.Value == SystemTestAttribute.Category
            );
        }

        [Fact]
        [UnitTest]
        public void SmokeTestAttributeTests()
        {
            var method = typeof(TestData.Traits).GetMethod(nameof(TestData.Traits.SmokeTest));

            var traits = TraitHelper.GetTraits(method!);

            traits.Should().Contain((kvp) =>

                kvp.Key == nameof(SmokeTestAttribute.Category) && kvp.Value == SmokeTestAttribute.Category
            );
        }
    }
}