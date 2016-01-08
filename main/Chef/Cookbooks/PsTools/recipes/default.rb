#
# Cookbook Name:: PsTools
# Recipe:: default
#
# Copyright (c) 2015 The Authors, All Rights Reserved.
# Declaring Variables

pstools_zip_filename = "PSTools.zip"
pstools_zip_url = "https://download.sysinternals.com/files/" + pstools_zip_filename
pstools_zip_path = "c:\\temp\\pstools.zip"
pstools_path = "c:\\temp\\pstools\\"
psexec_path = "c:\\temp\\pstools\\psexec.exe"	

directory "#{pstools_path}" do
	action :create
end

powershell_script 'Download PS Tools' do
	code <<-EOH
		ECHO "#{pstools_zip_url}"
		$Client = New-Object System.Net.WebClient
		$Client.DownloadFile( "#{pstools_zip_url}", "#{pstools_zip_path}")
		EOH
	guard_interpreter :powershell_script
	not_if { File.exists?(pstools_zip_path)}
end

powershell_script 'Unzip PS Tools' do
	code <<-EOH
		$shell = new-object -com shell.application
		$zip = $shell.NameSpace("#{pstools_zip_path}")
		foreach($item in $zip.items())
		{
	$shell.Namespace("#{pstools_path}").copyhere($item)
		}
		EOH
	guard_interpreter :powershell_script
	not_if { File.exists?("#{psexec_path}")}
end
