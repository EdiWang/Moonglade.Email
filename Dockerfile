FROM mcr.microsoft.com/azure-functions/dotnet:6.0 AS base
WORKDIR /home/site/wwwroot
EXPOSE 80

FROM mcr.microsoft.com/dotnet/core/sdk:6.0 AS build
WORKDIR /src

# Auto copy to prevent 996
COPY ./src/**/*.csproj ./
RUN for file in $(ls *.csproj); do mkdir -p ./${file%.*}/ && mv $file ./${file%.*}/; done

RUN dotnet restore "Moonglade.Notification.AzFunc/Moonglade.Notification.AzFunc.csproj"
COPY ./src .
WORKDIR "/src/Moonglade.Notification.AzFunc"
RUN dotnet build "Moonglade.Notification.AzFunc.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Moonglade.Notification.AzFunc.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /home/site/wwwroot
COPY --from=publish /app/publish .
ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true