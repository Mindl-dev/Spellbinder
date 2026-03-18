# SpellBinder Community Server — Docker image
# Usage:
#   docker build -t spellbinder .
#   docker run -p 10601:10601/udp -p 10602:10602/tcp spellbinder
#   docker run ... spellbinder --dev    # simple passwords (password = username)
#
# Or with docker-compose:
#   docker compose up                   # production (diceware passwords -> credentials.txt)
#   docker compose run spellbinder --dev  # dev mode

FROM docker.io/library/mono:latest AS build

WORKDIR /src
COPY . .

# Restore NUnit (not in packages.config), restore solution, build server + tests
RUN nuget install NUnit -Version 3.14.0 -OutputDirectory packages -Verbosity quiet && \
    nuget install NUnit.ConsoleRunner -Version 3.16.3 -OutputDirectory packages -Verbosity quiet && \
    nuget restore Spellbinder.sln -Verbosity quiet && \
    msbuild SpellServer/SpellServer.csproj \
        /p:Configuration=Debug /p:Platform=x86 \
        /verbosity:minimal /nologo && \
    msbuild SpellServer.Tests/SpellServer.Tests.csproj \
        /p:Configuration=Debug /p:Platform=AnyCPU \
        /verbosity:minimal /nologo

# Run tests — build fails if tests fail
RUN mono packages/NUnit.ConsoleRunner.3.16.3/tools/nunit3-console.exe \
        SpellServer.Tests/bin/Debug/SpellServer.Tests.dll \
        --noresult --noheader

FROM docker.io/library/debian:bookworm-slim AS runtime

# Install mono from official repo + mysql + python
RUN apt-get update && \
    apt-get install -y --no-install-recommends ca-certificates gnupg && \
    gpg --homedir /tmp --no-default-keyring --keyring /usr/share/keyrings/mono-official-archive-keyring.gpg \
        --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys 3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF && \
    echo "deb [signed-by=/usr/share/keyrings/mono-official-archive-keyring.gpg] https://download.mono-project.com/repo/debian stable-buster main" \
        > /etc/apt/sources.list.d/mono-official-stable.list && \
    apt-get update && \
    apt-get install -y --no-install-recommends \
        mono-runtime \
        libmono-system-data4.0-cil \
        libmono-system-configuration4.0-cil \
        libmono-system-core4.0-cil \
        libmono-system-xml4.0-cil \
        libmono-system-xml-linq4.0-cil \
        libmono-system-numerics4.0-cil \
        libmono-system-runtime-serialization4.0-cil \
        libmono-system-web4.0-cil \
        libmono-microsoft-csharp4.0-cil \
        libmono-system-data-datasetextensions4.0-cil \
        libmono-system-windows-forms4.0-cil \
        mariadb-server \
        python3 \
        python3-pip \
    && rm -rf /var/lib/apt/lists/*

RUN pip3 install --break-system-packages pymysql

WORKDIR /app

# Copy build output (code only — no copyrighted game content)
COPY --from=build /src/Build/Debug/ ./
# Copy setup scripts and tools
COPY --from=build /src/hash_passwords.py /src/eff_short_wordlist.txt ./
COPY --from=build /src/SpellServer/app.config ./SpellServer.exe.config

# Expose game ports + API
EXPOSE 10601/udp
EXPOSE 10602/tcp
EXPOSE 10603/tcp

# Entrypoint script handles MySQL init + server start
COPY docker-entrypoint.sh /docker-entrypoint.sh
RUN chmod +x /docker-entrypoint.sh

ENTRYPOINT ["/docker-entrypoint.sh"]
