#!/bin/bash
set -e

SDKVER=$(dotnet --version)
CSC="/usr/lib64/dotnet/sdk/$SDKVER/Roslyn/bincore/csc.dll"

dotnet exec "$CSC" \
  -target:library \
  -nostdlib \
  -runtimemetadataversion:v4.0.30319 \
  -noconfig \
  -nullable:enable \
  -out:bin/System.Private.CoreLib.dll \
  $(find System.Core -name "*.cs")
