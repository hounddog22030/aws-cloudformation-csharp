property :volume_name, String
property :drive_letter, String
property :iso_url, String
property :iso_filename, String
property :iso_path, String

action :download do
	powershell_script 'Download ISO' do
		code <<-EOH
			ECHO "Downloading #{iso_url}/#{iso_filename} to #{iso_path}/#{iso_filename}"
			$Client = New-Object System.Net.WebClient
			$Client.DownloadFile( "#{iso_url}/#{iso_filename}", "#{iso_path}/#{iso_filename}")
			EOH
			guard_interpreter :powershell_script
			not_if { ::File.exist?("#{iso_path}/#{iso_filename}")}
	end
	new_resource.updated_by_last_action(true)
end