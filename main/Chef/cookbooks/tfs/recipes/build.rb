include_recipe 'ec2helper'
include_recipe 'PsTools'
include_recipe 'tfs::install'

# Declaring Variables
LogFile = "#{Chef::Config['file_cache_path']}\\Configure-Team-Foundation-Server-STD.log"


tfsconfigure_exe_file = "C:\\Program Files\\Microsoft Team Foundation Server 14.0\\Tools\\TFSConfig.exe"

configurationFile = "#{Chef::Config['file_cache_path']}/build.ini"

template "#{configurationFile}" do
  source 'build.ini.erb'
  variables( :buildserver=> "#{ENV['COMPUTERNAME']}")
end

# Installing Team Foundation Server Standard.
execute 'Configure Team Foundation Server STD' do
	command "#{node[:PsTools][:exe_path]} -accepteula -h -u #{node[:domainAdmin][:name]} -p #{node[:domainAdmin][:password]} \"C:\\Program Files\\Microsoft Team Foundation Server 14.0\\Tools\\TFSConfig.exe\" unattend /configure /unattendfile:#{configurationFile}>#{LogFile}"
	not_if { File.exist?( LogFile ) }
end

# Open Firewall To Domain
powershell_script 'Firewall' do
	code <<-EOH
		New-NetFirewallRule -DisplayName "TFS Server" -Direction Inbound -Protocol TCP -LocalPort 8080 -Action allow -Profile Domain
		EOH
	guard_interpreter :powershell_script
end