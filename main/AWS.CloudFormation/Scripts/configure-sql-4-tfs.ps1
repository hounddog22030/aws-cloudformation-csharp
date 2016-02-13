param(
  [string]$domainName,
  [string]$accountName,
  [string]$password
)


[reflection.assembly]::LoadWithPartialName("Microsoft.SqlServer.Smo")
[reflection.assembly]::LoadWithPartialName("Microsoft.SqlServer.SqlWmiManagement")
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
    Write-Host "Login:$Login"
    $login.LoginType = [Microsoft.SqlServer.Management.Smo.LoginType]::WindowsUser
    Write-Host "Login:$Login"
    $login.Create()
    Write-Host "Login:$Login"
    $login.AddToRole('sysadmin')
    Write-Host "Login:$Login"
    $login.AddToRole('serveradmin')
    Write-Host "Login:$Login"


    $login2 = New-Object -TypeName Microsoft.SqlServer.Management.Smo.Login($Server, "$domainName\Domain Admins")
    Write-Host "Login:$login2"
    $login2.LoginType = [Microsoft.SqlServer.Management.Smo.LoginType]::WindowsUser
    Write-Host "Login:$login2"
    $login2.Create()
    Write-Host "Login:$login2"
    $login2.AddToRole('sysadmin')
    Write-Host "Login:$login2"
    $login2.AddToRole('serveradmin')
    Write-Host "Login:$login2"

}

[reflection.assembly]::LoadWithPartialName("Microsoft.SqlServer.SqlWmiManagement") | Out-Null
$mc = new-object Microsoft.SQLServer.Management.SMO.WMI.ManagedComputer localhost

$service = $mc.Services["MSSQLSERVER"]
$service.SetServiceAccount($accountName,$password)
$service.Alter()

# Load the assemblies
[reflection.assembly]::LoadWithPartialName("Microsoft.SqlServer.Smo")
[reflection.assembly]::LoadWithPartialName("Microsoft.SqlServer.SqlWmiManagement")

$smo = 'Microsoft.SqlServer.Management.Smo.'
$wmi = new-object ($smo + 'Wmi.ManagedComputer').

# List the object properties, including the instance names.
$Wmi

# Enable the TCP protocol on the default instance.
$uri = "ManagedComputer[@Name='" + (get-item env:\computername).Value + "']/ServerInstance[@Name='MSSQLSERVER']/ServerProtocol[@Name='Tcp']"
$Tcp = $wmi.GetSmoObject($uri)
$Tcp.IsEnabled = $true
$Tcp.Alter()
$Tcp



NET STOP MSSQLSERVER
NET START MSSQLSERVER
