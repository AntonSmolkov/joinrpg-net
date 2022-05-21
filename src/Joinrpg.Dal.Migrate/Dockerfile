FROM mcr.microsoft.com/dotnet/aspnet:6.0

# Add user/group with write permissions to work directory
RUN addgroup -g 1000 -S user && \
    adduser -u 1000 -S user -G user -h "/app"

WORKDIR /app

# Remove flags SUID/SGID
RUN chmod -s /app

# Set work user
USER user

COPY ./bin/Release/net6/publish/ .

ENTRYPOINT ["/app/Joinrpg.Dal.Migrate"]