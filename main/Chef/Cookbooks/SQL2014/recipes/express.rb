#
# Cookbook Name:: SQL_Server_2012_STD_x64
# Recipe:: default
#
# Copyright (c) 2014 Ryan Irujo, All Rights Reserved.

include_recipe "DotNet35"

# Declaring Variables
exe_name		  = "SQLEXPRWT_x64_ENU.exe"
iso_path          = "#{Chef::Config['file_cache_path']}\\" + exe_name
sql_svc_act       = "NT AUTHORITY\\NETWORK SERVICE"
sql_svc_pass      = ""
sql_sysadmins     = "#{ENV['USERDOMAIN']}\\Domain Admins"
sql_agent_svc_act = "NT Service\\SQLSERVERAGENT"

Chef::Log.fatal("sql_sysadmins:#{sql_sysadmins}")


s3_file iso_path do
	remote_path "/software/#{exe_name}"
	bucket "gtbb"
	aws_access_key_id "#{node[:s3_file][:key]}"
	aws_secret_access_key "#{node[:s3_file][:secret]}"
	s3_url "https://s3.amazonaws.com/gtbb"
	action :create
end

# Installing SQL Server 2014 Express.
execute "Installing SQL Server Express" do
	timeout 7200
	command "#{iso_path} /q /ACTION=Install /FEATURES=SQLEngine,SSMS /INSTANCENAME=MSSQLSERVER /SQLSVCACCOUNT=\"#{sql_svc_act}\" /SQLSYSADMINACCOUNTS=\"#{sql_sysadmins}\" \"BUILTIN\\USERS\" /AGTSVCACCOUNT=\"#{sql_agent_svc_act}\" /IACCEPTSQLSERVERLICENSETERMS /INDICATEPROGRESS /UpdateEnabled=False /SECURITYMODE=SQL /SAPWD=JUhsd82.!# /TCPENABLED=1 /ADDCURRENTUSERASSQLADMIN=true /SQLUSERDBDIR=\"d:\\sqldata\" /SQLUSERDBLOGDIR=\"e:\\sqllog\" /INSTALLSQLDATADIR=\"d:\\sqldata\""
	not_if '((gwmi -class win32_service | Where-Object {$_.Name -eq "MSSQLSERVER"}).Name -eq "MSSQLSERVER")'
end

# Open Firewall To Domain
powershell_script 'Firewall' do
	code <<-EOH
		New-NetFirewallRule -DisplayName "SQL Server" -Direction Inbound -Protocol TCP -LocalPort 1433 -Action allow -Profile Domain
		EOH
	guard_interpreter :powershell_script
end
