#
# Cookbook Name:: Team_Foundation_Server_STD_x64
# Recipe:: default
#
# Copyright (c) 2014 Ryan Irujo, All Rights Reserved.

include_recipe "TFS::iso"

# Declaring Variables
TFS_svc_act       = "NT AUTHORITY\\NETWORK SERVICE"
TFS_agent_svc_act = "NT Service\\TFSSERVERAGENT"
volume_name = "VS2013_4_TFS_EXP_ENU"
TFS_DomainAdminUserName = "#{node[:domainAdmin][:name]}"
TFS_DomainAdminPassword = "#{node[:domainAdmin][:password]}"
LogFile = "#{Chef::Config['file_cache_path']}\\Configure-Team-Foundation-Server-STD.log"


tfsconfigure_exe_file = "C:\\Program Files\\Microsoft Team Foundation Server 12.0\\Tools\\TFSConfig.exe"

psexec_path = "c:\\tools\\pstools\\psexec.exe"

cookbook_file "configbasic.ini" do
	path "#{Chef::Config['file_cache_path']}\\configbasic.ini"
	action :create_if_missing	
end

# Installing Team Foundation Server Standard.
execute 'Configure Team Foundation Server STD' do
	command "#{psexec_path} -accepteula -h -u #{TFS_DomainAdminUserName} -p #{TFS_DomainAdminPassword} \"C:\\Program Files\\Microsoft Team Foundation Server 12.0\\Tools\\TFSConfig.exe\" unattend /configure /unattendfile:#{Chef::Config['file_cache_path']}\\configbasic.ini>#{LogFile}"
	guard_interpreter :powershell_script
	not_if { File.exist?( LogFile ) }
end

# Open Firewall To Domain
powershell_script 'Firewall' do
	code <<-EOH
		New-NetFirewallRule -DisplayName "TFS Server" -Direction Inbound -Protocol TCP -LocalPort 8080 -Action allow -Profile Domain
		EOH
	guard_interpreter :powershell_script
end