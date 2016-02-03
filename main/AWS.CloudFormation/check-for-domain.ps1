Param(
  [string]$domain
)
Add-WindowsFeature RSAT-AD-PowerShell
$domainInfo = Get-ADDomain
if ($domainInfo.DNSRoot.ToLower() -eq $domain.ToLower())
{
    Write-Host "Already in domain: $domain"
    exit 1
}
else
{
    Write-Host "Needs to be added to domain: $domain"
    exit 0
}