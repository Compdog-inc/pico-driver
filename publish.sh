#!/bin/sh

# Initialize NodeJS project
cd ./webapp
npm ci          # install packages
npm run build   # build static webapp

cd ../

# Publish .NET project
dotnet publish DriverStation.csproj -c Release -r osx-x64 --sc -o bin/Release/net8.0/publish/osx-x64
