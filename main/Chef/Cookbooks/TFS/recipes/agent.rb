include_recipe 'ec2helper'
include_recipe 'PsTools'
include_recipe 's3_file'
include_recipe 'PsTools'
include_recipe 'tfs::build'

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
