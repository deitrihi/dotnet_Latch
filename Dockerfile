# Latch 서버 컨테이너 이미지 (linux/amd64 — UGREEN DXP2800 / Intel N100). 자녀 PC용 설치 패키지도 함께 빌드
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/Latch.Shared/Latch.Shared.csproj src/Latch.Shared/
COPY src/Latch.Server/Latch.Server.csproj src/Latch.Server/
RUN dotnet restore src/Latch.Server/Latch.Server.csproj
COPY src/Latch.Shared/ src/Latch.Shared/
COPY src/Latch.Server/ src/Latch.Server/
RUN dotnet publish src/Latch.Server/Latch.Server.csproj -c Release -o /app --no-restore

# 자녀 PC 설치 패키지: Agent + LockUI (win-x64 self-contained) + 설치 스크립트 → zip
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS agent
RUN apt-get update && apt-get install -y --no-install-recommends zip && rm -rf /var/lib/apt/lists/*
WORKDIR /src
COPY src/Latch.Shared/ src/Latch.Shared/
COPY src/Latch.Agent/ src/Latch.Agent/
COPY src/Latch.LockUI/ src/Latch.LockUI/
RUN for p in Latch.Agent Latch.LockUI; do \
      dotnet publish src/$p -c Release -r win-x64 --self-contained -p:EnableWindowsTargeting=true -o /pkg/Latch || exit 1; \
    done
COPY scripts/install-agent.bat scripts/install-agent.ps1 scripts/uninstall-agent.bat scripts/uninstall-agent.ps1 /pkg/Latch/
RUN cd /pkg && zip -qr latch-agent-win-x64.zip Latch

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
COPY --from=agent /pkg/latch-agent-win-x64.zip downloads/
# DB·데이터 보호 키 저장 위치. 비루트 사용자(app)가 쓸 수 있게 소유권 지정
RUN mkdir -p /data && chown $APP_UID /data
ENV ASPNETCORE_HTTP_PORTS=8080 \
    Latch__DataDir=/data
VOLUME /data
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "Latch.Server.dll"]
