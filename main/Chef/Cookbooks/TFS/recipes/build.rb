#
# Cookbook Name:: Team_Foundation_Server_STD_x64
# Recipe:: default
#
# Copyright (c) 2014 Ryan Irujo, All Rights Reserved.

include_recipe "TFS::iso"

# Declaring Variables
TFS_svc_act       = "NT AUTHORITY\\NETWORK SERVICE"
TFS_svc_pass      = ""
TFS_sysadmins     = "gtbb\\Domain Admins"
TFS_agent_svc_act = "NT Service\\TFSSERVERAGENT"
tfsconfigure_exe_file = "C:\\Program Files\\Microsoft Team Foundation Server 12.0\\Tools\\TFSConfig.exe"

psexec_path = "c:\\tools\\pstools\\psexec.exe"


cookbook_file "build.ini" do
	path "#{Chef::Config['file_cache_path']}\\build.ini"
	action :create_if_missing	
end

# Installing Team Foundation Server Standard.
execute 'Configure Team Foundation Server STD' do
	command "#{psexec_path} -accepteula -h -u gtbb\\johnny -p 3ORMSkQCxbNo \"C:\\Program Files\\Microsoft Team Foundation Server 12.0\\Tools\\TFSConfig.exe\" unattend /configure /unattendfile:#{Chef::Config['file_cache_path']}\\build.ini>#{Chef::Config['file_cache_path']}\\build-config.log"
	not_if { File.exist?("#{Chef::Config['file_cache_path']}\\build-config.log") }
end

# Open Firewall To Domain
powershell_script 'Firewall' do
	code <<-EOH
		New-NetFirewallRule -DisplayName "TFS Build" -Direction Inbound -Protocol TCP -LocalPort 9191 -Action allow -Profile Domain
		EOH
	guard_interpreter :powershell_script
end