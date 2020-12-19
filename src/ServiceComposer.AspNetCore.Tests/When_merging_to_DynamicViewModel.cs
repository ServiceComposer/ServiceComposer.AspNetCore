using FakeItEasy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Dynamic;
using Microsoft.Extensions.Logging;
using Xunit;

namespace ServiceComposer.AspNetCore.Tests
{
    public class When_merging_to_DynamicViewModel
    {
        [Fact]
        public void ExpandoObject_properties_should_be_copied_over()
        {
            // Arrange
            dynamic sut = new DynamicViewModel(A.Fake<ILogger<DynamicViewModel>>(), new CompositionContext("empty",A.Fake<RouteData>(), A.Fake<HttpRequest>()));
            dynamic source = new ExpandoObject();
            source.SomeProperty = "some value";

            // Act
            sut.Merge(source);

            // Assert
            Assert.Equal(source.SomeProperty, sut.SomeProperty);
        }

        [Fact]
        public void Existing_properties_should_be_overwritten()
        {
            // Arrange
            dynamic sut = new DynamicViewModel(A.Fake<ILogger<DynamicViewModel>>(), new CompositionContext("empty", A.Fake<RouteData>(), A.Fake<HttpRequest>()));
            sut.ExistingProperty = 10;

            dynamic source = new ExpandoObject();
            source.SomeProperty = "some value";
            source.ExistingProperty = 20;
            
            // Act
            sut.Merge(source);

            // Assert
            Assert.Equal(source.SomeProperty, sut.SomeProperty);
            Assert.Equal(source.ExistingProperty, sut.ExistingProperty);
        }
    }
}
