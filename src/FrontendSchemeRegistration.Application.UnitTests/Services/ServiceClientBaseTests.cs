namespace FrontendSchemeRegistration.Application.UnitTests.Services
{
	using Application.Services;
	using FluentAssertions;

	[TestFixture]
	public class ServiceClientBaseTests
	{
		public class TestDto
		{
			public string Name { get; set; }
			public int Age { get; set; }
			public string City { get; set; }
		}
		[Test]
		public void BuildUrlWithQueryString_ShouldReturnCorrectQueryString()
		{
			// Arrange
			var dto = new TestDto
			{
				Name = "John Doe",
				Age = 30,
				City = "New York"
			};
			// Act
			var result = ServiceClientBase.BuildUrlWithQueryString(dto);
			// Assert
			result.Should().Be("?Name=John%20Doe&Age=30&City=New%20York");
		}
		[Test]
		public void BuildUrlWithQueryString_ShouldHandleNullProperties()
		{
			// Arrange
			var dto = new TestDto
			{
				Name = "John Doe",
				Age = 30,
				City = null
			};
			// Act
			var result = ServiceClientBase.BuildUrlWithQueryString(dto);
			// Assert
			result.Should().Be("?Name=John%20Doe&Age=30");
		}
	}
}