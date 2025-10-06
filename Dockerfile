# Usa una imagen base de .NET SDK para la compilación
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /src

# Copiar el archivo csproj de la raíz del proyecto al contenedor
COPY ["Audicob.csproj", "./"]

# Restaurar las dependencias
RUN dotnet restore "Audicob.csproj"

# Copiar todo el contenido del proyecto
COPY . .

# Publicar la aplicación
RUN dotnet publish "Audicob.csproj" -c Release -o /app/publish

# Usar la imagen base de ASP.NET para la ejecución
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base

WORKDIR /app
EXPOSE 80

# Copiar los archivos publicados desde la etapa anterior
COPY --from=build /app/publish .

# Establecer el punto de entrada
ENTRYPOINT ["dotnet", "Audicob.dll"]
