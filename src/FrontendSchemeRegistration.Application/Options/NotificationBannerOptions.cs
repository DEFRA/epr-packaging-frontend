namespace FrontendSchemeRegistration.Application.Options;

using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public sealed record NotificationBannerOptions
{
    public const string Section = "FeatureOptions:NotificationBanner";

    public NotificationOption[] Notifications { get; init; } = [];

    public sealed record NotificationOption
    {
        /// <summary>
        ///     Root key of the language resource to use when rendering this notification.
        /// </summary>
        /// <remarks>
        ///     This is automatically suffixed with <c>_title</c>, <c>_header</c>, <c>_body</c>. Do not include those tokens.
        /// </remarks>
        public required string ResourceKey { get; init; }

        public DateTimeOffset EffectiveFrom { get; init; } = DateTimeOffset.MinValue;

        public DateTimeOffset EffectiveTo { get; init; } = DateTimeOffset.MaxValue;
    }
}