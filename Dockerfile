# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["SweetCakeShop/SweetCakeShop.csproj", "SweetCakeShop/"]
RUN dotnet restore "SweetCakeShop/SweetCakeShop.csproj"

# Copy the remaining files and build the app
COPY . .
WORKDIR "/src/SweetCakeShop"
RUN dotnet publish "SweetCakeShop.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS final
WORKDIR /app
COPY --from=build /app/publish .

# Render defaults container port to 8080 (standard for .NET 8+)
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "SweetCakeShop.dll"]
