FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# ĐÚNG: Vì Robot đang đứng ở trong folder Backend rồi
COPY ["UserService.Api.csproj", "./"] 
RUN dotnet restore "UserService.Api.csproj"

COPY . .
RUN dotnet build "UserService.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "UserService.Api.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "UserService.Api.dll"]