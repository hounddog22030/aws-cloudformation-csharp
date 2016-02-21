$Root = [ADSI]"LDAP://RootDSE"
$Root
$Search = "LDAP://CN=Users," + $Root.rootDomainNamingContext
$Search
$Container =  [ADSI]($Search)
$Container
$name = "marco"
$usr = $Container.Create("user","cn=$name")
$usr.Put("sAMAccountName",$name)
$usr.CommitChanges()

$Search = "LDAP://CN=" + $name + ",CN=Users," + $Root.rootDomainNamingContext
$Search
$objUser = [ADSI]$Search
$objUser
$objUser.SetPassword("i5A2sj*!")
$objUser.userAccountControl
$objUser.userAccountControl = 544
$objUser.userAccountControl
$objUser.CommitChanges()