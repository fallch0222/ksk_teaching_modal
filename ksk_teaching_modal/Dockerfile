FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["ksk_teaching_modal/ksk_teaching_modal.csproj", "ksk_teaching_modal/"]
RUN dotnet restore "ksk_teaching_modal/ksk_teaching_modal.csproj"
COPY . .
WORKDIR "/src/ksk_teaching_modal"
RUN dotnet build "ksk_teaching_modal.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ksk_teaching_modal.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ksk_teaching_modal.dll"]
