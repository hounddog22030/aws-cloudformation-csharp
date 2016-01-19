#
# Cookbook Name:: Team_Foundation_Server_STD_x64
# Recipe:: default
#
# Copyright (c) 2014 Ryan Irujo, All Rights Reserved.

include_recipe "ec2helper"

# Declaring Variables
TFS_svc_act       = "NT AUTHORITY\\NETWORK SERVICE"
TFS_agent_svc_act = "NT Service\\TFSSERVERAGENT"
tfsconfigure_exe_file = "C:\\Program Files\\Microsoft Team Foundation Server 14.0\\Tools\\TFSConfig.exe"
TFS_DomainAdminUserName = "#{node[:domainAdmin][:name]}"
TFS_DomainAdminPassword = "#{node[:domainAdmin][:password]}"
LogFile = "#{Chef::Config['file_cache_path']}/build-config.log"
BuildIni = "#{Chef::Config['file_cache_path']}/build.ini"


psexec_path = "c:\\tools\\pstools\\psexec.exe"


template "#{BuildIni}" do
  source 'build.ini.erb'
  variables( :buildserver=> "#{ENV['COMPUTERNAME']}")
end

# Installing Team Foundation Server Standard.
execute 'Configure Team Foundation Server STD' do
	command "#{psexec_path} -accepteula -h -u #{TFS_DomainAdminUserName} -p #{TFS_DomainAdminPassword} \"C:\\Program Files\\Microsoft Team Foundation Server 14.0\\Tools\\TFSConfig.exe\" unattend /configure /unattendfile:#{BuildIni}>#{LogFile}"
	not_if { File.exist?( LogFile ) }
end

# Open Firewall To Domain
powershell_script 'Firewall' do
	code <<-EOH
		New-NetFirewallRule -DisplayName "TFS Build" -Direction Inbound -Protocol TCP -LocalPort 9191 -Action allow -Profile Domain
		EOH
	guard_interpreter :powershell_script
end
