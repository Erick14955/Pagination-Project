FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["Pagination Project/Pagination Project.csproj", "Pagination Project/"]
RUN dotnet restore "Pagination Project/Pagination Project.csproj"

COPY . .
WORKDIR "/src/Pagination Project"

RUN dotnet publish "Pagination Project.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 10000

ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["sh", "-c", "dotnet Pagination_Project.dll --urls http://0.0.0.0:${PORT:-10000}"]