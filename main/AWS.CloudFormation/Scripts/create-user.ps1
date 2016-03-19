[CmdletBinding()]
param(
	[Parameter(Position=0, Mandatory=$true)]
	[System.String]
	$username,

	[Parameter(Position=1, Mandatory=$true)]
	[System.String]
	$password
)

$Root = [ADSI]"LDAP://RootDSE"
$Root
$Search = "LDAP://CN=Users," + $Root.rootDomainNamingContext
$Search
$Container =  [ADSI]($Search)
$Container

$usr = $Container.Create("user","cn=$username")
$usr.Put("sAMAccountName",$username)
$usr.CommitChanges()

$Search = "LDAP://CN=" + $username + ",CN=Users," + $Root.rootDomainNamingContext
$Search
$objUser = [ADSI]$Search
$objUser
$objUser.SetPassword($password)
$objUser.userAccountControl
$objUser.userAccountControl = 544
$objUser.userAccountControl
$objUser.CommitChanges()