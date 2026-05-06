FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Vì đã đứng trong folder Backend, ta copy trực tiếp file csproj
COPY ["UserService.Api.csproj", "./"]
RUN dotnet restore "UserService.Api.csproj"

# Copy toàn bộ code còn lại của Backend vào
COPY . .
RUN dotnet publish "UserService.Api.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# Render thường dùng port 8080 hoặc 10000, giữ nguyên 8080 là ok
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "UserService.Api.dll"]