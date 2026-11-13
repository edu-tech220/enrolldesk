FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY EnrollDesk.Web/EnrollDesk.Web.csproj EnrollDesk.Web/
RUN dotnet restore EnrollDesk.Web/EnrollDesk.Web.csproj

COPY EnrollDesk.Web/ EnrollDesk.Web/
WORKDIR /src/EnrollDesk.Web
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "EnrollDesk.Web.dll"]
