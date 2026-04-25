# Linux x64 test environment for terminal-snake.
# Builds an image targeting linux/amd64 so it can be run on an Apple Silicon
# Mac via Rosetta / QEMU emulation under Docker Desktop.

FROM --platform=linux/amd64 ubuntu:24.04

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
COPY bin/linux-x64/ /opt/terminal-snake/

RUN chmod +x /opt/terminal-snake/terminal-snake

CMD ["/bin/bash"]
