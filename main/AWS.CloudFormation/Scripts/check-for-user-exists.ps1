param(
  [string]$userName
)

Import-Module ActiveDirectory

$user = Get-ADUser $userName

if ($user -eq $null)
{
	exit 0
}
else
{
	exit 1
}
