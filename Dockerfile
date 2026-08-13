# Stage 1: Build Angular Frontend
FROM node:20 AS frontend-build
WORKDIR /app/frontend
COPY Common.Frontend/package*.json ./
RUN npm install
COPY Common.Frontend/ ./
RUN npm run build -- --configuration production

# Stage 2: Build .NET API Hub
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src
COPY ["nuget.config", "./"]
COPY ["nupkg/", "nupkg/"]
COPY ["Common.ServiceHub/Common.ServiceHub.csproj", "Common.ServiceHub/"]
COPY ["Common.Context/Common.Context.csproj", "Common.Context/"]
COPY ["Common.DMO/Common.DMO.csproj", "Common.DMO/"]
COPY ["Common.DTO/Common.DTO.csproj", "Common.DTO/"]
RUN dotnet restore "Common.ServiceHub/Common.ServiceHub.csproj"

COPY . .

# Copy compiled Angular app into wwwroot of Common.ServiceHub
COPY --from=frontend-build /app/frontend/dist/CommonFrontend/browser ./Common.ServiceHub/wwwroot

WORKDIR "/src/Common.ServiceHub"
RUN dotnet build "Common.ServiceHub.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Common.ServiceHub.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Common.ServiceHub.dll"]
