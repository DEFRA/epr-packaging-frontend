FROM defradigital/dotnetcore-development:dotnet8.0 AS base
USER root
 
# Expose the app on a defined port, configurable via a build argument
ARG PORT=3000
ENV ASPNETCORE_URLS=http://*:${PORT}
EXPOSE ${PORT}

FROM defradigital/dotnetcore-development:dotnet8.0 AS build
USER root
WORKDIR /src
COPY ["FrontendSchemeRegistration.UI/FrontendSchemeRegistration.UI.csproj", "FrontendSchemeRegistration.UI/"]
COPY ["FrontendSchemeRegistration.Application/FrontendSchemeRegistration.Application.csproj", "FrontendSchemeRegistration.Application/"]
COPY ["NuGet.Config",""]
 
# Use Build Argument to insert PAT into Nuget file
ARG PAT=localhost
RUN sed -i "s|</configuration>|<packageSourceCredentials><epr-packaging-common><add key=\"Username\" value=\"PAT\" /><add key=\"ClearTextPassword\" value=\"${PAT}\" /></epr-packaging-common></packageSourceCredentials></configuration>|" NuGet.Config
 
# Restore as distinct layers
COPY Directory.Build.props ./
RUN dotnet restore "FrontendSchemeRegistration.UI/FrontendSchemeRegistration.UI.csproj" --configfile "./NuGet.Config"
 
# Install NPM
USER root
RUN apk update && apk add --no-cache npm
 
ENV NODE_EXTRA_CA_CERTS=/usr/local/share/ca-certificates/internal-ca.crt
 
# Build and publish a release
COPY FrontendSchemeRegistration.UI/. ./FrontendSchemeRegistration.UI/.
COPY FrontendSchemeRegistration.Application/. ./FrontendSchemeRegistration.Application/.
WORKDIR "/src/FrontendSchemeRegistration.UI"
RUN dotnet build "FrontendSchemeRegistration.UI.csproj" -c Release -o /app/build
 
FROM build AS publish
RUN dotnet publish "FrontendSchemeRegistration.UI.csproj" -c Release -o /app/publish

RUN dotnet publish -c Release -o out

# Build runtime image
FROM defradigital/dotnetcore:dotnet8.0
COPY --from=build-env /home/dotnet/FrontendSchemeRegistration.UI/out .

# Install tzdata
USER root
RUN apk add --no-cache tzdata
 
# Add internationalisation support
USER root
RUN apk add --no-cache icu-libs icu-data-full libintl
 
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=0
FROM base AS final
 
USER dotnet
 
WORKDIR /app

COPY --from=publish /app/publish .
USER dotnet
ENTRYPOINT ["dotnet", "FrontendSchemeRegistration.UI.dll"]