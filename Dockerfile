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

# Cho phép OpenSSL sử dụng TLS cũ và các thuật toán mã hóa yếu (cần thiết cho Somee.com)
RUN sed -i 's/\[openssl_init\]/\[openssl_init\]\nssl_conf = ssl_sect/g' /etc/ssl/openssl.cnf || true && \
    printf "\n[ssl_sect]\nsystem_default = system_default_sect\n\n[system_default_sect]\nMinProtocol = TLSv1\nCipherString = DEFAULT@SECLEVEL=0\n" >> /etc/ssl/openssl.cnf || true

COPY --from=build /app/publish .

# Render defaults container port to 8080 (standard for .NET 8+)
ENV ASPNETCORE_URLS=http://+:8080
ENV DOTNET_USE_POLLING_FILE_WATCHER=true
ENV DOTNET_HOSTBUILDER__RELOADCONFIGONCHANGE=false
EXPOSE 8080

ENTRYPOINT ["dotnet", "SweetCakeShop.dll"]
