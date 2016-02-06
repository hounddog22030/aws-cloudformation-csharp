param(
  [string]$domainName
)

Import-Module SQLPS -DisableNameChecking


$Computer = $env:COMPUTERNAME
$SqlServices = Get-Service -DisplayName 'SQL Server (*'
Write-Host $SqlServices
$InstanceNames = $SqlServices.DisplayName.Split('(')[1].Trim(')')
Write-Host "InstanceNames:$InstanceNames"

foreach ($Instance in $InstanceNames) {
    Write-Host "Instance: $Instance"
    if ($Instance -eq 'MSSQLSERVER') {
            ## If a default instance only use the server name and don’t specify an instance
            $Server = new-object ('Microsoft.SqlServer.Management.Smo.Server') $Computer
    } else {
            ## If a named instance use the server name and the instance name
            $Server = new-object ('Microsoft.SqlServer.Management.Smo.Server') "$Computer`$Instance"
     }
    ## Output all Microsoft.SqlServer.Management.Smo.Login objects
    ##$Server.Logins
    
    #$Server = New-Object -TypeName Microsoft.SqlServer.Management.Smo.Server -ArgumentList $Instance
    Write-Host "Server:$Server"
    $login = New-Object -TypeName Microsoft.SqlServer.Management.Smo.Login($Server, "$domainName\Domain Computers")
    $login.LoginType = [Microsoft.SqlServer.Management.Smo.LoginType]::WindowsUser
    $login.Create()
    $login.AddToRole('sysadmin')
    $login.AddToRole('serveradmin')
    Write-Host "Login:$Login"

    ##$Login.LoginType = "WindowsUser"
}