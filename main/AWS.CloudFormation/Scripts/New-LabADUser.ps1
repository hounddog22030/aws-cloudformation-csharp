begin {
    $users = Import-Csv $PSScriptRoot\users.csv
}

process {
	$Root = [ADSI]"LDAP://RootDSE"
	$Root

	foreach ($user in $users) {
			$firstname = $user.firstname
			$lastname = $user.lastname
			$name = "$firstname $lastname"
			$alias = "$($firstname[0])$lastname".ToLower()
			$Search = "LDAP://CN=Users," + $Root.rootDomainNamingContext
			$Container =  [ADSI]($Search)
			$Container
			$usr = $Container.Create("user","cn=$alias")
			$usr.Put("sAMAccountName",$alias)
			$usr.CommitChanges()

			$Search = "LDAP://CN=" + $alias + ",CN=Users," + $Root.rootDomainNamingContext
			$Search
			$objUser = [ADSI]$Search
			$objUser
			$objUser.SetPassword('H3ll0!23$5!!')
			$objUser.userAccountControl
			$objUser.userAccountControl = 66048
			$objUser.userAccountControl
			$objUser.CommitChanges()
		}
}