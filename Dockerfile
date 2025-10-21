# =========================
# Build stage
# =========================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiamos csproj primero para aprovechar cache
COPY Cuidemoslos.sln ./
COPY Cuidemoslos.Domain/*.csproj Cuidemoslos.Domain/
COPY Cuidemoslos.BLL/*.csproj Cuidemoslos.BLL/
COPY Cuidemoslos.DAL/*.csproj Cuidemoslos.DAL/
COPY Cuidemoslos.Services/*.csproj Cuidemoslos.Services/
COPY Cuidemoslos.Web/*.csproj Cuidemoslos.Web/

RUN dotnet restore Cuidemoslos.Web/Cuidemoslos.Web.csproj

# Copiamos el resto del código y publicamos
COPY . .
RUN dotnet publish Cuidemoslos.Web/Cuidemoslos.Web.csproj -c Release -o /app/out

# =========================
# Runtime stage
# =========================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# La app escucha en 8080 (Render usa este puerto)
ENV ASPNETCORE_URLS=http://0.0.0.0:8080

COPY --from=build /app/out ./
EXPOSE 8080

ENTRYPOINT ["dotnet", "Cuidemoslos.Web.dll"]