# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER app
EXPOSE 8080
EXPOSE 8443

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/Services/UserService/UserService.API/UserService.API.csproj", "src/Services/UserService/UserService.API/"]
COPY ["src/Services/UserService/UserService.Domain/UserService.Domain.csproj", "src/Services/UserService/UserService.Domain/"]
COPY ["src/Services/UserService/UserService.Infrastructure/UserService.Infrastructure.csproj", "src/Services/UserService/UserService.Infrastructure/"]
COPY ["src/Shared/CommonLibrary/KidsMoneyNote.Shared.CommonLibrary/KidsMoneyNote.Shared.CommonLibrary.csproj", "src/Shared/CommonLibrary/KidsMoneyNote.Shared.CommonLibrary/"]
RUN dotnet restore "./src/Services/UserService/UserService.API/UserService.API.csproj"
COPY . .
WORKDIR "/src/src/Services/UserService/UserService.API"
RUN dotnet build "./UserService.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./UserService.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "UserService.API.dll"]