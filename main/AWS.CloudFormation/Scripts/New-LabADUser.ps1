begin {
    $users = Import-Csv $PSScriptRoot\users.csv
}

process {
	$Root = [ADSI]"LDAP://RootDSE"
	$Root

	foreach ($user in $users) {
			$firstname = $user.firstname
			$lastname = $user.lastname
			$principal = $user.principal
			$name = "$firstname $lastname"            Write-Host "principal=$principal"
			$Search = "LDAP://CN=Users," + $Root.rootDomainNamingContext
			$Container =  [ADSI]($Search)
			$Container
			$usr = $Container.Create("user","cn=$name")
			$usr.Put("sAMAccountName",$principal)
			$usr.CommitChanges()

			$Search = "LDAP://CN=" + $name + ",CN=Users," + $Root.rootDomainNamingContext
			$Search
			$objUser = [ADSI]$Search
			$objUser
			$password = $user.password
			Write-Host "password=$password"
			$objUser.SetPassword($password)
			$objUser.userAccountControl
			$objUser.userAccountControl = 66048
			$objUser.userAccountControl
			$objUser.CommitChanges()
		}
}