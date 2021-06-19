#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["src/Aguacongas.TheIdServer.BlazorApp/Aguacongas.TheIdServer.BlazorApp.csproj", "src/Aguacongas.TheIdServer.BlazorApp/"]
COPY ["src/BlazorApp/Aguacongas.TheIdServer.BlazorApp.Components/Aguacongas.TheIdServer.BlazorApp.Components.csproj", "src/BlazorApp/Aguacongas.TheIdServer.BlazorApp.Components/"]
COPY ["src/BlazorApp/Aguacongas.TheIdServer.BlazorApp.Infrastructure/Aguacongas.TheIdServer.BlazorApp.Infrastructure.csproj", "src/BlazorApp/Aguacongas.TheIdServer.BlazorApp.Infrastructure/"]
COPY ["src/BlazorApp/Aguacongas.TheIdServer.BlazorApp.Pages/Aguacongas.TheIdServer.BlazorApp.Pages.csproj", "src/BlazorApp/Aguacongas.TheIdServer.BlazorApp.Pages/"]
COPY ["src/BlazorApp/Aguacongas.TheIdServer.BlazorApp.Pages.Api/Aguacongas.TheIdServer.BlazorApp.Pages.Api.csproj", "src/BlazorApp/Aguacongas.TheIdServer.BlazorApp.Pages.Api/"]
COPY ["src/BlazorApp/Aguacongas.TheIdServer.BlazorApp.Pages.Apis/Aguacongas.TheIdServer.BlazorApp.Pages.Apis.csproj", "src/BlazorApp/Aguacongas.TheIdServer.BlazorApp.Pages.Apis/"]
COPY ["src/BlazorApp/Aguacongas.TheIdServer.BlazorApp.Pages.ApiScope/Aguacongas.TheIdServer.BlazorApp.Pages.ApiScope.csproj", "src/BlazorApp/Aguacongas.TheIdServer.BlazorApp.Pages.ApiScope/"]
COPY ["src/BlazorApp/Aguacongas.TheIdServer.BlazorApp.Pages.ApiScopes/Aguacongas.TheIdServer.BlazorApp.Pages.ApiScopes.csproj", "src/BlazorApp/Aguacongas.TheIdServer.BlazorApp.Pages.ApiScopes/"]
COPY ["src/BlazorApp/Aguacongas.TheIdServer.BlazorApp.Pages.Client/Aguacongas.TheIdServer.BlazorApp.Pages.Client.csproj", "src/BlazorApp/Aguacongas.TheIdServer.BlazorApp.Pages.Client/"]
COPY ["src/BlazorApp/Aguacongas.TheIdServer.BlazorApp.Pages.Clients/Aguacongas.TheIdServer.BlazorApp.Pages.Clients.csproj", "src/BlazorApp/Aguacongas.TheIdServer.BlazorApp.Pages.Clients/"]
COPY ["src/BlazorApp/Aguacongas.TheIdServer.BlazorApp.Pages.Culture/Aguacongas.TheIdServer.BlazorApp.Pages.Culture.csproj", "src/BlazorApp/Aguacongas.TheIdServer.BlazorApp.Pages.Culture/"]
COPY ["src/BlazorApp/Aguacongas.TheIdServer.BlazorApp.Pages.Cultures/Aguacongas.TheIdServer.BlazorApp.Pages.Cultures.csproj", "src/BlazorApp/Aguacongas.TheIdServer.BlazorApp.Pages.Cultures/"]
COPY ["src/BlazorApp/Aguacongas.TheIdServer.BlazorApp.Pages.ExternalProvider/Aguacongas.TheIdServer.BlazorApp.Pages.ExternalProvider.csproj", "src/BlazorApp/Aguacongas.TheIdServer.BlazorApp.Pages.ExternalProvider/"]
COPY ["src/BlazorApp/Aguacongas.TheIdServer.BlazorApp.Pages.ExternalProviders/Aguacongas.TheIdServer.BlazorApp.Pages.ExternalProviders.csproj", "src/BlazorApp/Aguacongas.TheIdServer.BlazorApp.Pages.ExternalProviders/"]
COPY ["src/BlazorApp/Aguacongas.TheIdServer.BlazorApp.Pages.Identities/Aguacongas.TheIdServer.BlazorApp.Pages.Identities.csproj", "src/BlazorApp/Aguacongas.TheIdServer.BlazorApp.Pages.Identities/"]
COPY ["src/BlazorApp/Aguacongas.TheIdServer.BlazorApp.Pages.Identity/Aguacongas.TheIdServer.BlazorApp.Pages.Identity.csproj", "src/BlazorApp/Aguacongas.TheIdServer.BlazorApp.Pages.Identity/"]
COPY ["src/BlazorApp/Aguacongas.TheIdServer.BlazorApp.Pages.Import/Aguacongas.TheIdServer.BlazorApp.Pages.Import.csproj", "src/BlazorApp/Aguacongas.TheIdServer.BlazorApp.Pages.Import/"]
COPY ["src/BlazorApp/Aguacongas.TheIdServer.BlazorApp.Pages.Keys/Aguacongas.TheIdServer.BlazorApp.Pages.Keys.csproj", "src/BlazorApp/Aguacongas.TheIdServer.BlazorApp.Pages.Keys/"]
COPY ["src/BlazorApp/Aguacongas.TheIdServer.BlazorApp.Pages.Role/Aguacongas.TheIdServer.BlazorApp.Pages.Role.csproj", "src/BlazorApp/Aguacongas.TheIdServer.BlazorApp.Pages.Role/"]
COPY ["src/BlazorApp/Aguacongas.TheIdServer.BlazorApp.Pages.Roles/Aguacongas.TheIdServer.BlazorApp.Pages.Roles.csproj", "src/BlazorApp/Aguacongas.TheIdServer.BlazorApp.Pages.Roles/"]
COPY ["src/BlazorApp/Aguacongas.TheIdServer.BlazorApp.Pages.User/Aguacongas.TheIdServer.BlazorApp.Pages.User.csproj", "src/BlazorApp/Aguacongas.TheIdServer.BlazorApp.Pages.User/"]
COPY ["src/BlazorApp/Aguacongas.TheIdServer.BlazorApp.Pages.Users/Aguacongas.TheIdServer.BlazorApp.Pages.Users.csproj", "src/BlazorApp/Aguacongas.TheIdServer.BlazorApp.Pages.Users/"]
COPY ["src/IdentityServer/Aguacongas.IdentityServer.Admin.Http.Store/Aguacongas.IdentityServer.Admin.Http.Store.csproj", "src/IdentityServer/Aguacongas.IdentityServer.Admin.Http.Store/"]
COPY ["src/IdentityServer/Aguacongas.IdentityServer.Store/Aguacongas.IdentityServer.Store.csproj", "src/IdentityServer/Aguacongas.IdentityServer.Store/"]
COPY ["src/Aguacongas.TheIdServer.BlazorApp/nginx.conf", "src/Aguacongas.TheIdServer.BlazorApp/"]
RUN dotnet workload install microsoft-net-sdk-blazorwebassembly-aot
RUN dotnet restore "src/Aguacongas.TheIdServer.BlazorApp/Aguacongas.TheIdServer.BlazorApp.csproj"
COPY . .
WORKDIR "/src/src/Aguacongas.TheIdServer.BlazorApp"
RUN dotnet build "Aguacongas.TheIdServer.BlazorApp.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Aguacongas.TheIdServer.BlazorApp.csproj" -c Release -o /app/publish

FROM nginx:alpine AS final
WORKDIR /usr/share/nginx/html
COPY --from=publish /app/publish/wwwroot .
COPY --from=build /src/src/Aguacongas.TheIdServer.BlazorApp/nginx.conf /etc/nginx/nginx.conf
