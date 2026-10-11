# Build the API
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# the project file is copied first, so the packages are restored only when it changes
COPY CommentsApi/CommentsApi.csproj CommentsApi/
RUN dotnet restore CommentsApi/CommentsApi.csproj

COPY CommentsApi/ CommentsApi/
RUN dotnet publish CommentsApi/CommentsApi.csproj -c Release -o /app --no-restore

# Run the API
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

# a font for the captcha pictures
RUN apt-get update \
    && apt-get install -y --no-install-recommends fonts-dejavu-core \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "CommentsApi.dll"]
