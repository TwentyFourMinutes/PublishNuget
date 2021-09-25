FROM mcr.microsoft.com/dotnet/sdk:6.0 as build-env
WORKDIR /app
COPY . ./
RUN dotnet publish ./PublishNuget.csproj -c Release -o out

LABEL maintainer="Twenty <office@twenty-four.dev>"
LABEL repository="https://github.com/TwentyFourMinutes/PublishNuget"
LABEL homepage="https://github.com/marketplace/actions/publishnuget"

LABEL com.github.actions.name="PublishNuget"
LABEL com.github.actions.description="GitHub action to automate publishing NuGet packages when project version changes."
LABEL com.github.actions.icon="package"
LABEL com.github.actions.color="green"

FROM mcr.microsoft.com/dotnet/sdk:6.0
COPY --from=build-env /app/out .
ENTRYPOINT [ "dotnet", "/PublishNuget.dll" ]