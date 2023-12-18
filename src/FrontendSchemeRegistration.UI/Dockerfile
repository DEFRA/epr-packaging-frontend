FROM defradigital/dotnetcore-development:latest AS build-env

# Expose the app on a defined port, configurable via a build argument
ARG PORT=3000
ENV ASPNETCORE_URLS=http://*:${PORT}
EXPOSE ${PORT}

# Copy everything
COPY --chown=dotnet FrontendSchemeRegistration.UI/*.csproj ./FrontendSchemeRegistration.UI/
COPY --chown=dotnet FrontendSchemeRegistration.Application/*.csproj ./FrontendSchemeRegistration.Application/
COPY --chown=dotnet NuGet.Config ./

# Use Build Argument to insert PAT into Nuget file
ARG PAT=localhost
RUN sed -i "s|</configuration>|<packageSourceCredentials><epr-packaging-common><add key=\"Username\" value=\"PAT\" /><add key=\"ClearTextPassword\" value=\"${PAT}\" /></epr-packaging-common></packageSourceCredentials></configuration>|" NuGet.Config

# Restore as distinct layers
COPY Directory.Build.props ./
RUN dotnet restore "FrontendSchemeRegistration.UI/FrontendSchemeRegistration.UI.csproj" --configfile "NuGet.Config"

# Install NPM
USER root
RUN apk update && apk add --no-cache npm
USER dotnet
ENV NODE_EXTRA_CA_CERTS=/usr/local/share/ca-certificates/internal-ca.crt

# Build and publish a release
COPY --chown=dotnet FrontendSchemeRegistration.UI/. ./FrontendSchemeRegistration.UI/.
COPY --chown=dotnet FrontendSchemeRegistration.Application/. ./FrontendSchemeRegistration.Application/.
WORKDIR /home/dotnet/FrontendSchemeRegistration.UI

RUN dotnet publish -c Release -o out

# Build runtime image
FROM defradigital/dotnetcore:latest
COPY --from=build-env --chown=dotnet /home/dotnet/FrontendSchemeRegistration.UI/out .

# Install tzdata
USER root
RUN apk add --no-cache tzdata
USER dotnet

# Add internationalisation support
USER root
RUN apk add icu-libs icu-data-full libintl
USER dotnet

ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=0

# Expose the app on a defined port, configurable via a build argument
ARG PORT=3000
ENV ASPNETCORE_URLS=http://*:${PORT}
EXPOSE ${PORT}

CMD dotnet FrontendSchemeRegistration.UI.dll