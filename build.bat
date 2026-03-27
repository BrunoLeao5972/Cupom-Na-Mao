@echo off
REM ============================================================
REM  Script de Build — Cupom Na Mão
REM  Requisito: .NET 8 SDK instalado
REM  https://dotnet.microsoft.com/download/dotnet/8.0
REM ============================================================

echo.
echo ============================================================
echo   Cupom Na Mao - Build Script
echo ============================================================
echo.

REM Verificar se o .NET SDK está instalado
dotnet --version >nul 2>&1
if %ERRORLEVEL% NEQ 0 (
    echo [ERRO] .NET SDK nao encontrado.
    echo Instale o .NET 8 SDK em: https://dotnet.microsoft.com/download/dotnet/8.0
    pause
    exit /b 1
)

echo [OK] .NET SDK encontrado.
echo.

REM Restaurar pacotes NuGet
echo [1/3] Restaurando pacotes NuGet...
dotnet restore CupomNaMao\CupomNaMao.csproj
if %ERRORLEVEL% NEQ 0 (
    echo [ERRO] Falha ao restaurar pacotes.
    pause
    exit /b 1
)
echo [OK] Pacotes restaurados.
echo.

REM Build de release
echo [2/3] Compilando em modo Release...
dotnet build CupomNaMao\CupomNaMao.csproj -c Release --no-restore
if %ERRORLEVEL% NEQ 0 (
    echo [ERRO] Falha na compilacao.
    pause
    exit /b 1
)
echo [OK] Compilacao concluida.
echo.

REM Publicar como executável único auto-contido
echo [3/3] Publicando executavel unico...
dotnet publish CupomNaMao\CupomNaMao.csproj ^
    -c Release ^
    -r win-x64 ^
    --self-contained true ^
    -p:PublishSingleFile=true ^
    -p:PublishReadyToRun=true ^
    -p:IncludeNativeLibrariesForSelfExtract=true ^
    -o publish\

if %ERRORLEVEL% NEQ 0 (
    echo [ERRO] Falha na publicacao.
    pause
    exit /b 1
)

echo.
echo ============================================================
echo   BUILD CONCLUIDO COM SUCESSO!
echo   Executavel gerado em: publish\CupomNaMao.exe
echo ============================================================
echo.
pause
