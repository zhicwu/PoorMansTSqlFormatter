#!/bin/bash
rm -f $1SqlFormatter.exe

mono "../../../ExternalBuildTools/ILRepack/ILRepack.exe" /t:exe /out:SqlFormatterTemp.exe SqlFormatterExeAssembly.exe PoorMansTSqlFormatterLib.dll SybaseTSqlFormatterLib.dll NDesk.Options.dll LinqBridge.dll es/SqlFormatterExeAssembly.resources.dll

mono "../../../ExternalBuildTools/ILRepack/ILRepack.exe" /t:exe /out:SqlFormatter.exe SqlFormatterTemp.exe fr/SqlFormatterExeAssembly.resources.dll

rm -f $1LinqBridge.dll
rm -f $1NDesk.Options.dll
rm -f $1PoorMansTSqlFormatterLib.*
rm -f $1SybaseTSqlFormatterLib.*
rm -f $1SqlFormatterExeAssembly.*
rm -f $1SqlFormatterTemp.*
rm -rf $1es
rm -rf $1fr

exit 0