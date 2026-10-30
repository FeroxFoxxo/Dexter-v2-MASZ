FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build-env
WORKDIR /backend

COPY ./ ./
RUN dotnet publish -c Release -o ./bin

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /backend/bin
COPY --from=build-env /backend/bin/ .

ENV TZ=America/New_York
RUN ln -snf /usr/share/zoneinfo/$TZ /etc/localtime && echo $TZ > /etc/timezone

ENTRYPOINT ["dotnet", "Launch.dll"]
EXPOSE 80
