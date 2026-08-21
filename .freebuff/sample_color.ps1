Add-Type -AssemblyName System.Drawing

$srcPng = 'd:\sc\cnb\SurvivalcraftTool\SCAssistant\src\SCAssistant.UnoApp\SCAssistant.UnoApp\Assets\Icon.png'
$bmp = New-Object System.Drawing.Bitmap($srcPng)
$w = $bmp.Width; $h = $bmp.Height
"size: ${w}x${h}"

# 采样四个角 + 边缘的平均色
$corners = @{
  'top-left'     = $bmp.GetPixel(5, 5)
  'top-right'    = $bmp.GetPixel($w - 5, 5)
  'bottom-left'  = $bmp.GetPixel(5, $h - 5)
  'bottom-right' = $bmp.GetPixel($w - 5, $h - 5)
  'center'       = $bmp.GetPixel([int]($w/2), [int]($h/2))
}
foreach ($k in $corners.Keys) {
  $c = $corners[$k]
  '{0,-14} R={1,-3} G={2,-3} B={3,-3} A={4}' -f $k, $c.R, $c.G, $c.B, $c.A
}

# 边缘像素平均色（每边采样 50 个点）
$edge = @()
for ($i = 0; $i -lt 50; $i++) {
  $x = [int](($w - 1) * $i / 49)
  $y = [int](($h - 1) * $i / 49)
  $edge += $bmp.GetPixel($x, 2)      # 顶部
  $edge += $bmp.GetPixel($x, $h - 3) # 底部
  $edge += $bmp.GetPixel(2, $y)      # 左侧
  $edge += $bmp.GetPixel($w - 3, $y) # 右侧
}
$sr = 0; $sg = 0; $sb = 0; $sa = 0
foreach ($c in $edge) { $sr += $c.R; $sg += $c.G; $sb += $c.B; $sa += $c.A }
$n = $edge.Count
'edge avg: R={0} G={1} B={2} A={3}' -f [int]($sr/$n), [int]($sg/$n), [int]($sb/$n), [int]($sa/$n)
$bmp.Dispose()