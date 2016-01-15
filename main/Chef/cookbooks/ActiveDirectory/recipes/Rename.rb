#
# Cookbook Name:: ActiveDirectory
# Recipe:: Rename
#
# Copyright (c) 2015 The Authors, All Rights Reserved.


powershell_script 'Rename' do
	code <<-EOH
		$out = Rename-Computer -NewName #{node[:ActiveDirectory][:name]} -Restart
		$out >> #{Chef::Config['file_cache_path']}\\Rename.log
		EOH
	guard_interpreter :powershell_script
	not_if { File.exist?("#{Chef::Config['file_cache_path']}\\Rename.log") }
end