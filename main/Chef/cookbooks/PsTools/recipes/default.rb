#
# Cookbook Name:: PsTools
# Recipe:: default
#
# Copyright (c) 2015 The Authors, All Rights Reserved.
# Declaring Variables


directory "#{node[:PsTools][:dir]}" do
	action :create
end

puts "#{node[:PsTools][:zip_url]}"

powershell_script 'Download PS Tools' do
	code <<-EOH
		ECHO "Downloading pstools from #{node[:PsTools][:zip_url]}"
		$Client = New-Object System.Net.WebClient
		$Client.DownloadFile( "#{node[:PsTools][:zip_url]}", "#{node[:PsTools][:zip_path]}")
		EOH
	guard_interpreter :powershell_script
	not_if { File.exists?("#{node[:PsTools][:zip_path]}")}
end

powershell_script 'Unzip PS Tools' do
	code <<-EOH
		$shell = new-object -com shell.application
		$zip = $shell.NameSpace("#{node[:PsTools][:zip_path]}")
		foreach($item in $zip.items())
		{
			$shell.Namespace("#{node[:PsTools][:zip_path]}").copyhere($item)
		}
		EOH
	guard_interpreter :powershell_script
	not_if { File.exists?("#{node[:PsTools][:exe_path]}")}
end
