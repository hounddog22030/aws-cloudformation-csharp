#
# Cookbook Name:: ActiveDirectory
# Recipe:: Rename
#
# Copyright (c) 2015 The Authors, All Rights Reserved.

powershell_script 'DNS' do
	code <<-EOH
		Get-NetAdapter | Set-DnsClientServerAddress -ServerAddresses \"#{node[:ActiveDirectory][:DC1PrivateIp]},#{node[:ActiveDirectory][:DC2PrivateIp]}\"
		EOH
	guard_interpreter :powershell_script
end

powershell_script 'AddToDomain' do
	code "Add-Computer -DomainName #{node[:ActiveDirectory][:DomainDNSName]} -Credential (New-Object System.Management.Automation.PSCredential(\'#{node[:ActiveDirectory][:DomainNetBIOSName]}\\#{node[:ActiveDirectory][:DomainAdminUser]}\',(ConvertTo-SecureString #{node[:ActiveDirectory][:DomainAdminPassword]} -AsPlainText -Force))) -Restart >> #{Chef::Config['file_cache_path']}\\AddToDomain.log"
	guard_interpreter :powershell_script
	not_if { File.exist?("#{Chef::Config['file_cache_path']}\\AddToDomain.log") }
end

