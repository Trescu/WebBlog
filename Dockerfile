FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY backend/WebBlog.Api/WebBlog.Api.csproj backend/WebBlog.Api/
RUN dotnet restore backend/WebBlog.Api/WebBlog.Api.csproj

COPY . .
RUN dotnet publish backend/WebBlog.Api/WebBlog.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "WebBlog.Api.dll"]