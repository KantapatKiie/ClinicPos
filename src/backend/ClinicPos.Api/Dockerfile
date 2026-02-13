FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/backend/ClinicPos.Api/ClinicPos.Api.csproj src/backend/ClinicPos.Api/
RUN dotnet restore src/backend/ClinicPos.Api/ClinicPos.Api.csproj
COPY src/backend/ClinicPos.Api src/backend/ClinicPos.Api
WORKDIR /src/src/backend/ClinicPos.Api
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "ClinicPos.Api.dll"]
