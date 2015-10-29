@echo off
pushd "%~dp0"
call build && NuGet pack Worms.nuspec -Symbol
popd
