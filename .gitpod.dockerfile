FROM gitpod/workspace-dotnet

USER root

RUN wget -q https://packages.microsoft.com/config/ubuntu/19.04/packages-microsoft-prod.deb && \
    dpkg -i packages-microsoft-prod.deb && rm -rf packages-microsoft-prod.deb && \
    add-apt-repository universe && \
    apt-get update && apt-get -y -o APT::Install-Suggests="true" install aspnetcore-runtime-3.1 && apt-get -y -o APT::Install-Suggests="true" install dotnet-runtime-3.1 && \
    rm -rf /var/lib/apt/lists/*;