#
# Cookbook Name:: sqlserver
# Recipe:: default
#
# Copyright (C) 2016 YOUR_NAME
#
# All rights reserved - Do Not Redistribute
#

include_recipe 'ec2helper'

drive_letter = "S:"
exe_path = "#{drive_letter}/setup.exe"

ec2helper_mount 'MountEc2Drives' do
	volume_name "SQL2014_x64_ENU"
	drive_letter "#{drive_letter}"
	notifies :run, 'execute[Install SQL]', :immediately
	action :mount
end

#	command "#{exe_path} /q /ACTION=Install /FEATURES=SQLEngine,SSMS /INSTANCENAME=MSSQLSERVER /SQLSVCACCOUNT=\"NT AUTHORITY\\NETWORK SERVICE\" /SQLSYSADMINACCOUNTS=\"#{ENV['USERDOMAIN']}\\Domain Admins\" \"BUILTIN\\USERS\" /AGTSVCACCOUNT=\"NT Service\\SQLSERVERAGENT\" /IACCEPTSQLSERVERLICENSETERMS /INDICATEPROGRESS /UpdateEnabled=False /SECURITYMODE=SQL /SAPWD=JUhsd82.!# /TCPENABLED=1 /ADDCURRENTUSERASSQLADMIN=true /SQLUSERDBDIR=\"d:\\sqldata\" /SQLUSERDBLOGDIR=\"e:\\sqllog\" /INSTALLSQLDATADIR=\"d:\\sqldata\""
execute 'Install SQL' do
	timeout 7200
	command "#{exe_path} /q /ACTION=Install /FEATURES=SQLEngine,SSMS /INSTANCENAME=MSSQLSERVER /IAcceptSQLServerLicenseTerms=true /SQLSVCACCOUNT=\"NT AUTHORITY\\NETWORK SERVICE\" /SQLSYSADMINACCOUNTS=\"#{ENV['USERDOMAIN']}\\Domain Admins\" /AGTSVCACCOUNT=\"NT Service\\SQLSERVERAGENT\" /IACCEPTSQLSERVERLICENSETERMS /INDICATEPROGRESS /UpdateEnabled=False /SECURITYMODE=SQL /SAPWD=JUhsd82.!# /TCPENABLED=1 /ADDCURRENTUSERASSQLADMIN=true /SQLUSERDBDIR=\"d:\\sqldata\" /SQLUSERDBLOGDIR=\"e:\\sqllog\" /INSTALLSQLDATADIR=\"d:\\sqldata\""
	not_if { File.exist?("C:\\Program Files\\Microsoft SQL Server\\MSSQL12.MSSQLSERVER\\MSSQL\\Binn\\sqlserv.exe") }
	action :nothing
end
