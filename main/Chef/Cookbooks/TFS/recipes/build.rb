include_recipe 'ec2helper'
include_recipe 'PsTools'
include_recipe 'tfs::install'
include_recipe 's3_file'
include_recipe 'PsTools'

# Declaring Variables
LogFile = "#{Chef::Config['file_cache_path']}\\Configure-Team-Foundation-Server-STD.log"


tfsconfigure_exe_file = "C:\\Program Files\\Microsoft Team Foundation Server 14.0\\Tools\\TFSConfig.exe"

configurationFile = "#{Chef::Config['file_cache_path']}/build.ini"

template "#{configurationFile}" do
  source 'build.ini.erb'
  variables(	:build_server_netbios_name => "#{ENV['COMPUTERNAME']}",
				:application_server_netbios_name => "#{node[:tfs][:application_server_netbios_name]}")
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

s3_file "#{node[:tfs][:build_agent_zipfile_path]}" do
    remote_path "/#{node[:tfs][:build_agent_zipfile_filename]}"
    bucket "#{node[:tfs][:build_agent_zipfile_bucket_name]}"
    aws_access_key_id "#{node[:s3_file][:key]}"
    aws_secret_access_key "#{node[:s3_file][:secret]}"
    s3_url "#{node[:tfs][:build_agent_zipfile_bucket_url]}"
    action :create
	notifies :unzip, "windows_zipfile[#{node[:tfs][:build_agent_dir]}]", :immediately
end

windows_zipfile "#{node[:tfs][:build_agent_dir]}" do
	source "#{node[:tfs][:build_agent_zipfile_path]}"
	not_if { File.exist?("#{node[:tfs][:build_agent_command_file_path]}") }
	action :nothing
	notifies :run, 'execute[InstallAgent]', :immediately
end

LogFileInstallAgent = "#{Chef::Config['file_cache_path']}/InstallAgent.log"

execute 'InstallAgent' do
	command "#{node[:PsTools][:exe_path]} -accepteula -h -u #{node[:domainAdmin][:name]} -p #{node[:domainAdmin][:password]} #{node[:tfs][:build_agent_command_file_path]} /ServerUrl:http://#{node[:tfs][:application_server_netbios_name]}:8080/tfs /Configure /Name:#{ENV['COMPUTERNAME']} /force /RunningAsService /PoolName:default  /WindowsServiceLogonAccount:\"#{node[:domainAdmin][:name]}\" /WindowsServiceLogonPassword:\"#{node[:domainAdmin][:password]}\" /NoPrompt > #{LogFileInstallAgent}"
	action :nothing
	not_if { File.exist?( LogFileInstallAgent ) }
end

s3_file "#{node[:tfs][:build_test_agent_path]}" do
    remote_path "/#{node[:tfs][:build_test_agent_filename]}"
    bucket "#{node[:tfs][:build_test_agent_bucket_name]}"
    aws_access_key_id "#{node[:s3_file][:key]}"
    aws_secret_access_key "#{node[:s3_file][:secret]}"
    s3_url "#{node[:tfs][:build_test_agent_bucket_url]}"
    action :create
	notifies :run, "execute[InstallMsTestAgent]", :immediately
end

LogFileInstallTestAgent = "#{Chef::Config['file_cache_path']}/TestAgent.log"

execute 'InstallMsTestAgent' do
	command "#{node[:tfs][:build_test_agent_path]} /s /q > #{LogFileInstallTestAgent}"
	action :nothing
	not_if { File.exist?( LogFileInstallTestAgent ) }
end

