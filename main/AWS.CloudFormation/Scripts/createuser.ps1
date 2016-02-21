$Root = [ADSI]"LDAP://RootDSE"
$Root
$Search = "LDAP://CN=Users," + $Root.rootDomainNamingContext
$Search
$Container =  [ADSI]($Search)
$Container
$name = "marcorubio"
$usr = $Container.Create("user","cn=$name")
$usr.Put("sAMAccountName",$name)

$usr.CommitChanges()

#$usr2 = [ADSI]"LDAP://cn=" + $name + ",cn=Users"
#$usr2
#$usr2.SetPassword("kjdkjsdkjf123.")
#$usr2.CommitChanges()
#CN=azassasads221szf,CN=Users,DC=upsilon,DC=dev,DC=yadayadasoftware,DC=com
$Search = "LDAP://CN=" + $name + ",CN=Users," + $Root.rootDomainNamingContext
$Search
$objUser = [ADSI]$Search
$objUser
# ("LDAP://cn=aassasads221zf,ou=Management,dc=NA,dc=fabrikam,dc=com")
$objUser.SetPassword("i5A2sj*!")
$objUser.userAccountControl
$objUser.userAccountControl = 544
$objUser.userAccountControl
$objUser.CommitChanges()