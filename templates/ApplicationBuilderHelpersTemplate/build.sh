#!/bin/bash

echo "Bash version $BASH_VERSION"

set -eo pipefail
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

###########################################################################
# CONFIGURATION
###########################################################################

BUILD_PROJECT_FILE="$SCRIPT_DIR/build/_build.csproj"
TEMP_DIRECTORY="$SCRIPT_DIR/.nuke/temp"

DOTNET_GLOBAL_FILE="$SCRIPT_DIR/global.json"
DOTNET_INSTALL_URL="https://dot.net/v1/dotnet-install.sh"
DOTNET_CHANNEL="STS"

export DOTNET_SYSTEM_CONSOLE_ALLOW_ANSI_COLOR_REDIRECTION=1
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_MULTILEVEL_LOOKUP=0
export DOTNET_NOLOGO=1
export DOTNET_ROLL_FORWARD="Major"
export NUKE_TELEMETRY_OPTOUT=1

###########################################################################
# EXECUTION
###########################################################################

function FirstJsonValue {
    perl -nle 'print $1 if m{"'"$1"'"\s*:\s*"([^"]+)"}' <<< "${@:2}"
}

# If dotnet CLI is installed globally and it matches requested version, use for execution
if [ -x "$(command -v dotnet)" ] && dotnet --version &>/dev/null; then
    export DOTNET_EXE="$(command -v dotnet)"
else
    # Download install script
    DOTNET_INSTALL_FILE="$TEMP_DIRECTORY/dotnet-install.sh"
    mkdir -p "$TEMP_DIRECTORY"
    curl -Lsfo "$DOTNET_INSTALL_FILE" "$DOTNET_INSTALL_URL"
    chmod +x "$DOTNET_INSTALL_FILE"

    # If global.json exists, load expected version
    if [ -f "$DOTNET_GLOBAL_FILE" ]; then
        DOTNET_VERSION=$(FirstJsonValue "version" "$(cat "$DOTNET_GLOBAL_FILE")")
    fi

    # Install by channel or specific version
    DOTNET_DIRECTORY="$TEMP_DIRECTORY/dotnet-unix"
    if [ -n "$DOTNET_VERSION" ]; then
        "$DOTNET_INSTALL_FILE" --install-dir "$DOTNET_DIRECTORY" --version "$DOTNET_VERSION" --no-path
    else
        "$DOTNET_INSTALL_FILE" --install-dir "$DOTNET_DIRECTORY" --channel "$DOTNET_CHANNEL" --no-path
    fi
    export DOTNET_EXE="$DOTNET_DIRECTORY/dotnet"
fi

echo "Microsoft (R) .NET SDK version $("$DOTNET_EXE" --version)"

if [ -f "$BUILD_PROJECT_FILE" ]; then
    "$DOTNET_EXE" build "$BUILD_PROJECT_FILE" -f net10.0 /nodeReuse:false /p:UseSharedCompilation=false -nologo -clp:NoSummary --verbosity quiet
    "$DOTNET_EXE" run --project "$BUILD_PROJECT_FILE" -f net10.0 --no-build -- "$@"
else
    echo "Build project not found at $BUILD_PROJECT_FILE"
    echo "Skipping NUKE build, running dotnet build directly..."
    "$DOTNET_EXE" build "$SCRIPT_DIR"
fi
