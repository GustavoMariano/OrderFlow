# Stage 1 - Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["OrderFlow.Api/OrderFlow.Api.csproj", "OrderFlow.Api/"]
COPY ["OrderFlow.Application/OrderFlow.Application.csproj", "OrderFlow.Application/"]
COPY ["OrderFlow.Domain/OrderFlow.Domain.csproj", "OrderFlow.Domain/"]
COPY ["OrderFlow.Infrastructure/OrderFlow.Infrastructure.csproj", "OrderFlow.Infrastructure/"]
COPY ["OrderFlow.Contracts/OrderFlow.Contracts.csproj", "OrderFlow.Contracts/"]

RUN dotnet restore "OrderFlow.Api/OrderFlow.Api.csproj"

COPY . .
WORKDIR "/src/OrderFlow.Api"
RUN dotnet publish -c Release -o /app/publish

# Stage 2 - Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "OrderFlow.Api.dll"]
