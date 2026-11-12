#!/bin/bash
# postCreateCommand: fetch the GameDev-MCP-Server binary the Unity-MCP plugin talks to.
# UNITY_MCP_SERVER_VERSION must equal McpServerManager.ServerVersion of the
# com.ivanmurzak.unity.mcp version pinned in UnityProject/Packages/manifest.json;
# a mismatched pair may fail to connect.
set -euo pipefail

UNITY_MCP_SERVER_VERSION="9.5.1"
UNITY_MCP_SERVER_SHA256="853a67ebdb36688b6c56ad4c61852c387565a47c21d75339b522f486159072ac"
DIR="/workspace/.unity-mcp"
BIN="$DIR/linux-x64/gamedev-mcp-server"
STAMP="$DIR/version"

if [ -x "$BIN" ] && [ "$(cat "$STAMP" 2>/dev/null)" = "$UNITY_MCP_SERVER_VERSION" ]; then
  echo "gamedev-mcp-server $UNITY_MCP_SERVER_VERSION already present"
  exit 0
fi

echo "Downloading gamedev-mcp-server $UNITY_MCP_SERVER_VERSION..."
rm -rf "$DIR" && mkdir -p "$DIR"
ZIP="$DIR/server.zip"
curl -fsSL -o "$ZIP" \
  "https://github.com/IvanMurzak/GameDev-MCP-Server/releases/download/v$UNITY_MCP_SERVER_VERSION/gamedev-mcp-server-linux-x64.zip"
echo "$UNITY_MCP_SERVER_SHA256  $ZIP" | sha256sum -c -
# The zip's top-level entry is linux-x64/, so extracting into $DIR yields $BIN
unzip -o -q "$ZIP" -d "$DIR"
rm "$ZIP"
chmod +x "$BIN"
echo "$UNITY_MCP_SERVER_VERSION" > "$STAMP"
echo "gamedev-mcp-server $UNITY_MCP_SERVER_VERSION installed"
