Get-ChildItem 'C:\Users\YYLMZXC\.nuget\packages\uno.resizetizer' -Recurse -Include '*.targets','*.props' -File |
    ForEach-Object { $_.FullName }