# ============================================================
#  Script de Build — Cupom Na Mão (PowerShell)
#  Requisito: .NET 8 SDK instalado
#  https://dotnet.microsoft.com/download/dotnet/8.0
# ============================================================

Write-Host ""
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "  Cupom Na Mao - Build Script (PowerShell)" -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host ""

# Verificar .NET SDK
try {
    $dotnetVersion = dotnet --version
    Write-Host "[OK] .NET SDK encontrado: $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Host "[ERRO] .NET SDK nao encontrado." -ForegroundColor Red
    Write-Host "Instale em: https://dotnet.microsoft.com/download/dotnet/8.0"
    Read-Host "Pressione Enter para sair"
    exit 1
}

Write-Host ""

# Restaurar pacotes
Write-Host "[1/3] Restaurando pacotes NuGet..." -ForegroundColor Yellow
dotnet restore "CupomNaMao\CupomNaMao.csproj"
if ($LASTEXITCODE -ne 0) {
    Write-Host "[ERRO] Falha ao restaurar pacotes." -ForegroundColor Red
    Read-Host "Pressione Enter para sair"
    exit 1
}
Write-Host "[OK] Pacotes restaurados." -ForegroundColor Green
Write-Host ""

# Build
Write-Host "[2/3] Compilando em modo Release..." -ForegroundColor Yellow
dotnet build "CupomNaMao\CupomNaMao.csproj" -c Release --no-restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "[ERRO] Falha na compilacao." -ForegroundColor Red
    Read-Host "Pressione Enter para sair"
    exit 1
}
Write-Host "[OK] Compilacao concluida." -ForegroundColor Green
Write-Host ""

# Publicar
Write-Host "[3/3] Publicando executavel unico..." -ForegroundColor Yellow
dotnet publish "CupomNaMao\CupomNaMao.csproj" `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:PublishReadyToRun=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -o "publish\"

if ($LASTEXITCODE -ne 0) {
    Write-Host "[ERRO] Falha na publicacao." -ForegroundColor Red
    Read-Host "Pressione Enter para sair"
    exit 1
}

Write-Host ""
Write-Host "============================================================" -ForegroundColor Green
Write-Host "  BUILD CONCLUIDO COM SUCESSO!" -ForegroundColor Green
Write-Host "  Executavel: publish\CupomNaMao.exe" -ForegroundColor Green
Write-Host "============================================================" -ForegroundColor Green
Write-Host ""
Read-Host "Pressione Enter para fechar"
