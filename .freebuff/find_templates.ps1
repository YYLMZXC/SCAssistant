$pkgDir = Join-Path $env:USERPROFILE '.nuget\packages'
Get-ChildItem $pkgDir -Directory | Where-Object { $_.Name -like 'uno*' } | ForEach-Object { $_.Name }