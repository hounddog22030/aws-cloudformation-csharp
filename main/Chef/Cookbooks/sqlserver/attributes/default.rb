default['sqlserver']['iso_volume_name'] = "SQLEXPRADV_x64_ENU"
default['sqlserver']['setup_drive_letter'] = "invalid"
default['sqlserver']['setup_drive_letter_filename'] = "c:/cfn/files/#{node[:sqlserver][:iso_volume_name]}.driveletter"

default['sqlserver']['FEATURES'] = 'SQLEngine,SSMS,FullText'
default['sqlserver']['INSTANCENAME'] = 'MSSQLSERVER'
default['sqlserver']['IAcceptSQLServerLicenseTerms'] = 'true'
default['sqlserver']['SQLSVCACCOUNT'] = 'NT AUTHORITY\\NETWORK SERVICE'
default['sqlserver']['AGTSVCACCOUNT'] = 'NT Service\\SQLSERVERAGENT'
default['sqlserver']['SECURITYMODE'] = 'SQL'
default['sqlserver']['SAPWD'] = 'JUhsd82.!#'
default['sqlserver']['TCPENABLED'] = '1'
default['sqlserver']['ADDCURRENTUSERASSQLADMIN'] = 'false'
default['sqlserver']['SQLUSERDBDIR'] = 'c:\\sqldata'
default['sqlserver']['SQLUSERDBLOGDIR'] = 'c:\\sqllog'
default['sqlserver']['INSTALLSQLDATADIR'] = 'c:\\sqldata'

if "#{ENV['USERDOMAIN']}" == "#{ENV['COMPUTERNAME']}"
	default['sqlserver']['SQLSYSADMINACCOUNTS'] = "\"#{ENV['COMPUTERNAME']}\\Users\""
else
	default['sqlserver']['SQLSYSADMINACCOUNTS'] = "\"#{node[:domain]}\\Domain Users\" \"#{node[:domain]}\\Domain Computers\""
end



