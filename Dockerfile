FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source
COPY src/CatalogoJogos.Api/CatalogoJogos.Api.csproj src/CatalogoJogos.Api/
RUN dotnet restore src/CatalogoJogos.Api/CatalogoJogos.Api.csproj
COPY src/ src/
RUN dotnet publish src/CatalogoJogos.Api/CatalogoJogos.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "CatalogoJogos.Api.dll"]
