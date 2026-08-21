Add-Type -AssemblyName System.Drawing

$srcPng = 'd:\sc\cnb\SurvivalcraftTool\SCAssistant\src\SCAssistant.UnoApp\SCAssistant.UnoApp\Assets\Icon.png'
$fgSvg = 'd:\sc\cnb\SurvivalcraftTool\SCAssistant\src\SCAssistant.UnoApp\SCAssistant.UnoApp\Assets\Icons\icon_foreground.svg'
$bgSvg = 'd:\sc\cnb\SurvivalcraftTool\SCAssistant\src\SCAssistant.UnoApp\SCAssistant.UnoApp\Assets\Icons\icon_background.svg'

$img = [System.Drawing.Image]::FromFile($srcPng)
$w = $img.Width; $h = $img.Height
$img.Dispose()

# 1. 前景：完整 Icon.png（base64 内嵌）
$bytes = [System.IO.File]::ReadAllBytes($srcPng)
$b64 = [System.Convert]::ToBase64String($bytes, [System.Base64FormattingOptions]::InsertLineBreaks)
$fg = '<svg width="{0}" height="{1}" viewBox="0 0 {0} {1}" xmlns="http://www.w3.org/2000/svg">' -f $w, $h
$fg += "<image x=`"0`" y=`"0`" width=`"{0}`" height=`"{1}`" href=`"data:image/png;base64,`n{2}`"/>" -f $w, $h, $b64
$fg += '</svg>'
$fg += "`n"
[System.IO.File]::WriteAllText($fgSvg, $fg, (New-Object System.Text.UTF8Encoding($false)))

# 2. 背景：纯色（采样自 Icon.png 边缘平均色 #F2F2F0）
$bg = '<svg width="{0}" height="{1}" viewBox="0 0 {0} {1}" xmlns="http://www.w3.org/2000/svg">' -f $w, $h
$bg += '<rect x="0" y="0" width="{0}" height="{1}" fill="#F2F2F0"/>' -f $w, $h
$bg += '</svg>'
$bg += "`n"
[System.IO.File]::WriteAllText($bgSvg, $bg, (New-Object System.Text.UTF8Encoding($false)))

"icon_foreground.svg -> $fgSvg ($((Get-Item $fgSvg).Length) bytes)"
"icon_background.svg  -> $bgSvg ($((Get-Item $bgSvg).Length) bytes)"