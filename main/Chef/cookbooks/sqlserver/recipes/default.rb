#
# Cookbook Name:: sqlserver
# Recipe:: default
#
# Copyright (C) 2016 YOUR_NAME
#
# All rights reserved - Do Not Redistribute
#


puts node['domain']
puts node['system']['short_hostname']
puts 'domain'
puts node['system']['domain_name']
puts 'domain'
puts 'netbios_name'
puts node['system']['netbios_name']
puts 'netbios_name'
puts 'workgroup'
puts node['system']['workgroup']
puts 'workgroup'

include_recipe 'ec2helper'
include_recipe 'msdotnet35'


drive_letter = "S:"
exe_path = "#{drive_letter}/setup.exe"

ec2helper_mount 'MountEc2Drives' do
	volume_name "SQLEXPRADV_x64_ENU"
	drive_letter "#{drive_letter}"
	notifies :run, 'execute[Install SQL]', :immediately
	action :mount
end

#	command "#{exe_path} /q /ACTION=Install /FEATURES=SQLEngine,SSMS /INSTANCENAME=MSSQLSERVER /SQLSVCACCOUNT=\"NT AUTHORITY\\NETWORK SERVICE\" /SQLSYSADMINACCOUNTS=\"#{ENV['USERDOMAIN']}\\Domain Admins\" \"BUILTIN\\USERS\" /AGTSVCACCOUNT=\"NT Service\\SQLSERVERAGENT\" /IACCEPTSQLSERVERLICENSETERMS /INDICATEPROGRESS /UpdateEnabled=False /SECURITYMODE=SQL /SAPWD=JUhsd82.!# /TCPENABLED=1 /ADDCURRENTUSERASSQLADMIN=true /SQLUSERDBDIR=\"d:\\sqldata\" /SQLUSERDBLOGDIR=\"e:\\sqllog\" /INSTALLSQLDATADIR=\"d:\\sqldata\""
# S:/setup.exe /q /ACTION=Install /FEATURES=SQLEngine,SSMS /INSTANCENAME=MSSQLSERVER /IAcceptSQLServerLicenseTerms=true /SQLSVCACCOUNT="NT AUTHORITY\NETWORK SERVICE" /AGTSVCACCOUNT="NT Service\SQLSERVERAGENT" /IACCEPTSQLSERVERLICENSETERMS /INDICATEPROGRESS /UpdateEnabled=False /SECURITYMODE=SQL /SAPWD=JUhsd82.!# /TCPENABLED=1 /ADDCURRENTUSERASSQLADMIN=true /SQLUSERDBDIR=c:\sqldata /SQLUSERDBLOGDIR=c:\sqllog /INSTALLSQLDATADIR=c:\sqldata
SQLSYSADMINACCOUNTS = ''

if node['domain']
	SQLSYSADMINACCOUNTS = "/SQLSYSADMINACCOUNTS=\"#{node[:domain]}\\Domain Admins\""
end

puts "SQLSYSADMINACCOUNTS=#{SQLSYSADMINACCOUNTS}"



#default['sqlserver']['FEATURES'] = 'SQLEngine,SSMS'
#default['sqlserver']['INSTANCENAME'] = 'MSSQLSERVER'
#default['sqlserver']['IAcceptSQLServerLicenseTerms'] = 'true'
#default['sqlserver']['SQLSVCACCOUNT'] = 'NT AUTHORITY\\NETWORK SERVICE'
#default['sqlserver']['AGTSVCACCOUNT'] = 'NT Service\\SQLSERVERAGENT'
#default['sqlserver']['SECURITYMODE'] = 'SQL'
#default['sqlserver']['SAPWD'] = 'JUhsd82.!#'
#default['sqlserver']['TCPENABLED'] = '1'
#default['sqlserver']['ADDCURRENTUSERASSQLADMIN'] = 'false'
#default['sqlserver']['SQLUSERDBDIR'] = 'd:\\sqldata'
#default['sqlserver']['SQLUSERDBLOGDIR'] = 'c:\\sqllog'
#default['sqlserver']['INSTALLSQLDATADIR'] = 'd:\\sqldata'

execute 'Install SQL' do
	timeout 7200
	command "#{exe_path} /q #{SQLSYSADMINACCOUNTS} /ACTION=Install /FEATURES=#{node[:sqlserver][:FEATURES]} /INSTANCENAME=#{node[:sqlserver][:INSTANCENAME]} /IAcceptSQLServerLicenseTerms=#{node[:sqlserver][:IAcceptSQLServerLicenseTerms]} /SQLSVCACCOUNT=\"#{node[:sqlserver][:SQLSVCACCOUNT]}\" /AGTSVCACCOUNT=\"#{node[:sqlserver][:AGTSVCACCOUNT]}\" /INDICATEPROGRESS /UpdateEnabled=False /SECURITYMODE=#{node[:sqlserver][:SECURITYMODE]} /SAPWD=#{node[:sqlserver][:SAPWD]} /TCPENABLED=#{node[:sqlserver][:TCPENABLED]} /ADDCURRENTUSERASSQLADMIN=#{node[:sqlserver][:ADDCURRENTUSERASSQLADMIN]} /SQLUSERDBDIR=\"#{node[:sqlserver][:SQLUSERDBDIR]}\" /SQLUSERDBLOGDIR=\"#{node[:sqlserver][:SQLUSERDBLOGDIR]}\" /INSTALLSQLDATADIR=\"#{node[:sqlserver][:INSTALLSQLDATADIR]}\""
	not_if { File.exist?("C:\\Program Files\\Microsoft SQL Server\\MSSQL13.#{node[:sqlserver][:INSTANCENAME]}\\MSSQL\\Binn\\sqlserv.exe") }
	action :nothing
end
