FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props ./
COPY email-hub.sln ./
COPY Domain/Domain.csproj Domain/
COPY Contracts/Contracts.csproj Contracts/
COPY DTO/DTO.csproj DTO/
COPY DataAccess/DataAccess.csproj DataAccess/
COPY Application/Application.csproj Application/
COPY External/External.csproj External/
COPY Web/Web.csproj Web/

RUN dotnet restore Web/Web.csproj

COPY . .

RUN dotnet publish Web/Web.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Web.dll"]
