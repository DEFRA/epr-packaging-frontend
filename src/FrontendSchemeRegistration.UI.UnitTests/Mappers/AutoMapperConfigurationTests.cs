namespace FrontendSchemeRegistration.UI.UnitTests.Mappers;

using AutoMapper;

public class AutoMapperConfigurationTests
{
    [Test]
    public void Configuration_is_valid()
    {
        var configuration = new MapperConfiguration(cfg =>
        {
            var profiles = typeof(Program).Assembly.GetTypes()
                .Where(t => t.IsAssignableFrom(typeof(Profile)) && !t.IsAbstract);

            foreach (var profile in profiles)
                cfg.AddProfile(Activator.CreateInstance(profile) as Profile);
        });

        configuration.AssertConfigurationIsValid();
    }
}