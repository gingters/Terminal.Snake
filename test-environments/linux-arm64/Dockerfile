# Linux arm64 test environment for terminal-snake.
# Targets linux/arm64 so it runs natively on Apple Silicon.

FROM --platform=linux/arm64 ubuntu:24.04

RUN apt-get update \
 && apt-get install -y --no-install-recommends \
        ca-certificates \
        libstdc++6 \
        zlib1g \
        libssl3t64 \
        libkrb5-3 \
        ncurses-term \
        bash \
        less \
 && rm -rf /var/lib/apt/lists/*

ENV TERM=xterm-256color \
    LANG=C.UTF-8 \
    LC_ALL=C.UTF-8 \
    PATH=/opt/terminal-snake:${PATH}

WORKDIR /opt/terminal-snake

# Populated by the host before `docker build` (see ../fetch-release.sh).
COPY bin/linux-arm64/ /opt/terminal-snake/

RUN chmod +x /opt/terminal-snake/terminal-snake

CMD ["/bin/bash"]
