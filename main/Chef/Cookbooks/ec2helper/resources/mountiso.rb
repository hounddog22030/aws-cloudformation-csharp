property :volume_name, String
property :drive_letter, String
property :iso_url, String
property :iso_filename, String
property :iso_path, String

iso_fullpath_local = "#{iso_path}/#{iso_filename}"
iso_fullpath_remote = "#{iso_url}/#{iso_filename}"

action :download do
	powershell_script 'Download ISO' do
		code <<-EOH
			ECHO "Downloading #{iso_fullpath_remote} to #{iso_fullpath_local}"
			$Client = New-Object System.Net.WebClient
			$Client.DownloadFile( "#{iso_fullpath_remote}", "#{iso_fullpath_local}")
			EOH
		guard_interpreter :powershell_script
		not_if { File.exists?(iso_fullpath_local)}
	end
	new_resource.updated_by_last_action(true)
end