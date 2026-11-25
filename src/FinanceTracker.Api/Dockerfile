FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/FinanceTracker.Api/FinanceTracker.Api.csproj", "src/FinanceTracker.Api/"]
RUN dotnet restore "src/FinanceTracker.Api/FinanceTracker.Api.csproj"
COPY . .
WORKDIR "/src/src/FinanceTracker.Api"
RUN dotnet publish "FinanceTracker.Api.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "FinanceTracker.Api.dll"]
