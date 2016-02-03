Import-Module ActiveDirectory
Param(
  [string]$userName
)

$user = Get-ADUser $userName

if ($user -eq $null)
{
	exit 0
}
else
{
	exit 1
}
