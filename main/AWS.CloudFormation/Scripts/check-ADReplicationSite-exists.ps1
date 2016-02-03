Param(
  [string]$subnetName
)

$site = Get-ADReplicationSite $subnetName

if ($site -eq $null)
{
	exit 0
}
else
{
	exit 1
}
