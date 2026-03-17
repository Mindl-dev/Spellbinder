# SpellBinder Community Server — Docker image
# Usage:
#   docker build -t spellbinder .
#   docker run -p 10601:10601/udp -p 10602:10602/tcp spellbinder
#   docker run ... spellbinder --dev    # simple passwords (password = username)
#
# Or with docker-compose:
#   docker compose up                   # production (diceware passwords -> credentials.txt)
#   docker compose run spellbinder --dev  # dev mode

FROM mono:latest AS build

WORKDIR /src
COPY . .

# Restore and build
RUN nuget restore Spellbinder.sln -Verbosity quiet && \
    msbuild SpellServer/SpellServer.csproj \
        /p:Configuration=Debug /p:Platform=x86 \
        /verbosity:minimal /nologo

FROM mono:latest AS runtime

RUN apt-get update && \
    apt-get install -y --no-install-recommends \
        mysql-server \
        python3 \
        python3-pip \
    && rm -rf /var/lib/apt/lists/*

RUN pip3 install --break-system-packages pymysql 2>/dev/null || pip3 install pymysql

WORKDIR /app

# Copy build output (code only — no copyrighted game content)
COPY --from=build /src/Build/Debug/ ./
# Copy setup scripts and tools
COPY --from=build /src/hash_passwords.py /src/eff_short_wordlist.txt ./
COPY --from=build /src/SpellServer/app.config ./SpellServer.exe.config

# Expose game ports
EXPOSE 10601/udp
EXPOSE 10602/tcp

# Entrypoint script handles MySQL init + server start
COPY docker-entrypoint.sh /docker-entrypoint.sh
RUN chmod +x /docker-entrypoint.sh

ENTRYPOINT ["/docker-entrypoint.sh"]
