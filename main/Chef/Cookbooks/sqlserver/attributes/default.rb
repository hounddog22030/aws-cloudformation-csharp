﻿default['sqlserver']['FEATURES'] = 'SQLEngine,SSMS'
default['sqlserver']['INSTANCENAME'] = 'MSSQLSERVER'
default['sqlserver']['IAcceptSQLServerLicenseTerms'] = 'true'
default['sqlserver']['SQLSVCACCOUNT'] = 'NT AUTHORITY\\NETWORK SERVICE'
default['sqlserver']['AGTSVCACCOUNT'] = 'NT Service\\SQLSERVERAGENT'
default['sqlserver']['SECURITYMODE'] = 'SQL'
default['sqlserver']['SAPWD'] = 'JUhsd82.!#'
default['sqlserver']['TCPENABLED'] = '1'
default['sqlserver']['ADDCURRENTUSERASSQLADMIN'] = 'false'
default['sqlserver']['SQLUSERDBDIR'] = 'd:\\sqldata'
default['sqlserver']['SQLUSERDBLOGDIR'] = 'c:\\sqllog'
default['sqlserver']['INSTALLSQLDATADIR'] = 'd:\\sqldata'

if "#{ENV['USERDOMAIN']}" == "#{ENV['COMPUTERNAME']}"
	default['sqlserver']['SQLSYSADMINACCOUNTS'] = "\"#{ENV['COMPUTERNAME']}\\Users\""
else
	default['sqlserver']['SQLSYSADMINACCOUNTS'] = "\"#{node[:domain]}\\Domain Users\" \"#{node[:domain]}\\Domain Computers\""
end


