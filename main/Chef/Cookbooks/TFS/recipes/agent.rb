include_recipe 'ec2helper'
include_recipe 'PsTools'
include_recipe 's3_file'
include_recipe 'PsTools'

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

cmd = "#{node[:tfs][:build_agent_command_file_path]} /ServerUrl:http://#{node[:tfs][:application_server_netbios_name]}:8080/tfs /Configure /Name:#{ENV['COMPUTERNAME']} /force /RunningAsService /PoolName:default  /WindowsServiceLogonAccount:\"#{node[:tfs][:build_agent_account_name]}\" /WindowsServiceLogonPassword:\"#{node[:tfs][:build_agent_password]}\" /NoPrompt"

puts "#{cmd}"

execute 'InstallAgent' do
	command cmd
	action :nothing
end

machineConfigX86 = "C:/Windows/Microsoft.NET/Framework/v4.0.30319/Config/machine.config"

template "#{machineConfigX86}" do
  source 'build.x86.machine.config'
  variables(	:sqlexpress4build_private_dns_name => "#{node[:tfs][:sqlexpress4build_private_dns_name]}",
				:sqlexpress4build_username => "#{node[:tfs][:sqlexpress4build_username]}",
				:sqlexpress4build_password => "#{node[:tfs][:sqlexpress4build_password]}" )
end
