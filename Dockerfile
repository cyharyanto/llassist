FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
# Copy csproj and restore as distinct layers
COPY ["llassist.ApiService/llassist.ApiService.csproj", "llassist.ApiService/"]
COPY ["llassist.Common/llassist.Common.csproj", "llassist.Common/"]
# If there are other projects, copy their csproj files here as well

# Restore dependencies
RUN dotnet restore "llassist.ApiService/llassist.ApiService.csproj"
# Copy everything else and build
COPY . .
WORKDIR "/src/llassist.ApiService"
RUN dotnet build "llassist.ApiService.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
RUN dotnet publish "llassist.ApiService.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "llassist.ApiService.dll"]