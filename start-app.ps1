# file start-app.ps1
Write-Host "============================" -ForegroundColor Cyan
Write-Host "KHOI DONG HEO THONG" -ForegroundColor Green
Write-Host "============================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Dang bat Backend..." -ForegroundColor Yellow
Start-Process -FilePath "dotnet" -ArgumentList "run" -WorkingDirectory "$PSScriptRoot\backend\OlapAnalytics.API" -WindowStyle Normal

Write-Host "Dang mo Frontend UI..." -ForegroundColor Yellow
Start-Process -FilePath "npm" -ArgumentList "run dev" -WorkingDirectory "$PSScriptRoot\frontend" -WindowStyle Normal

Write-Host ""
Write-Host "Thanh cong! cac cua so da duoc nap." -ForegroundColor Cyan
