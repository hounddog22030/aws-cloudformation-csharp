Import-Module ActiveDirectory
Param(
  [string]$subnetCidr
)

$site = Get-ADReplicationSubnet -Identity $subnetCidr

if ($site -eq $null)
{
	exit 0
}
else
{
	exit 1
}
