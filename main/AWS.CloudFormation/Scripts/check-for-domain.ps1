Import-Module ActiveDirectory
if ((gwmi win32_computersystem).partofdomain -eq $true) {
    Write-Host "Already in domain: $domain"
    exit 1
} else {
    Write-Host "Needs to be added to domain: $domain"
    exit 0
}
