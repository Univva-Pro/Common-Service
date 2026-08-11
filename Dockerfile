FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src
COPY ["nuget.config", "./"]
COPY ["nupkg/", "nupkg/"]
COPY ["Common.ServiceHub/Common.ServiceHub.csproj", "Common.ServiceHub/"]
COPY ["Common.Context/Common.Context.csproj", "Common.Context/"]
COPY ["Common.DMO/Common.DMO.csproj", "Common.DMO/"]
COPY ["Common.DTO/Common.DTO.csproj", "Common.DTO/"]
RUN dotnet restore "Common.ServiceHub/Common.ServiceHub.csproj"

# Copy source code
COPY . .

WORKDIR "/src/Common.ServiceHub"
RUN dotnet build "Common.ServiceHub.csproj" -c Debug -o /app/build

FROM build AS publish
RUN dotnet publish "Common.ServiceHub.csproj" -c Debug -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Common.ServiceHub.dll"]
