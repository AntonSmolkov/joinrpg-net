FROM mcr.microsoft.com/dotnet/aspnet:6.0

# Add user/group with write permissions to work directory
RUN addgroup -g 1000 -S user && \
    adduser -u 1000 -S user -G user -h "/app"

WORKDIR /app

#We need this to make ClosedXML work
RUN apt-get update && apt-get install -y apt-utils libgdiplus libc6-dev

COPY ./bin/Release/net6/publish/ .

# Remove flags SUID/SGID
RUN chmod -s /app

# Set work user
USER user

ENV ASPNETCORE_URLS="http://+:8080"

EXPOSE 8080/tcp

ENTRYPOINT ["/app/JoinRpg.Portal"]