begin {
    $users = Import-Csv $PSScriptRoot\users.csv
}

process {
	$Root = [ADSI]"LDAP://RootDSE"
	$Root

	foreach ($user in $users) {
			$firstname = $user.firstname
			$lastname = $user.lastname
			$principal = $users.principal
			$name = "$firstname $lastname"
			$Search = "LDAP://CN=Users," + $Root.rootDomainNamingContext
			$Container =  [ADSI]($Search)
			$Container
			$usr = $Container.Create("user","cn=$name")
			$usr.Put("sAMAccountName",$principal)
			$usr.CommitChanges()

			$Search = "LDAP://CN=" + $principal + ",CN=Users," + $Root.rootDomainNamingContext
			$Search
			$objUser = [ADSI]$Search
			$objUser
			$password = $users.password
			Write-Host "password=$password"
			$objUser.SetPassword('H3ll0!23$5!!')
			$objUser.userAccountControl
			$objUser.userAccountControl = 66048
			$objUser.userAccountControl
			$objUser.CommitChanges()
		}
}