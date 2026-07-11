FROM mcr.microsoft.com/dotnet/sdk:8.0

WORKDIR /src

COPY ["SportsReservationAPI.Tests/SportsReservationAPI.Tests.csproj", "SportsReservationAPI.Tests/"]
COPY ["SportsReservationAPI/SportsReservationAPI.csproj", "SportsReservationAPI/"]
RUN dotnet restore "SportsReservationAPI.Tests/SportsReservationAPI.Tests.csproj"

COPY . .

WORKDIR /src/SportsReservationAPI.Tests
ENTRYPOINT ["dotnet", "test", "--logger", "console;verbosity=normal"]
