param(
  [string]$path,
  [string]$shareName,
  [string[]]$fullAccess 
  
)
$exists = Test-Path $path
$exists
if (-not $exists )
{
	New-Item $path -type directory
}

New-SMBShare -Path $path -Name $shareName -FullAccess $fullAccess
