# Set the base image as the .NET 5.0 SDK (this includes the runtime)
FROM mcr.microsoft.com/dotnet/sdk:6.0 as build-env

# Copy everything and publish the release (publish implicitly restores and builds)
COPY . ./
RUN dotnet publish ./PublishNuget/PublishNuget.csproj -c Release -o out --no-self-contained

# Label the container
LABEL maintainer="Twenty <office@twenty-four.dev>"
LABEL repository="https://github.com/dotnet/samples" #TODO
LABEL homepage="https://github.com/dotnet/samples" #TODO

# Label as GitHub action
LABEL com.github.actions.name="PublishNuget"
# Limit to 160 characters
LABEL com.github.actions.description="GitHub action to automate publishing NuGet packages when project version changes."
# See branding:
# https://docs.github.com/actions/creating-actions/metadata-syntax-for-github-actions#branding
LABEL com.github.actions.icon="package"
LABEL com.github.actions.color="green"

# Relayer the .NET SDK, anew with the build output
FROM mcr.microsoft.com/dotnet/runtime:6.0
COPY --from=build-env /out .
ENTRYPOINT [ "dotnet", "/DotNet.GitHubAction.dll" ]