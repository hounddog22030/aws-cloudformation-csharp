#
# Cookbook Name:: msdotnet35
# Recipe:: default
#
# Copyright (c) 2015 The Authors, All Rights Reserved.

powershell_script 'Install' do
	code <<-EOH
		Install-WindowsFeature Net-Framework-Core
		EOH
	guard_interpreter :powershell_script
	not_if { File.exist?("C:/Windows/Microsoft.NET/Framework64/v3.5") }
end
