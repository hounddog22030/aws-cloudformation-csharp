property :volume_name, String
property :drive_letter, String
property :iso_filename, String
property :iso_path, String
property :drive_letter_filepath, String


action :mount do
	powershell_script 'Mount Team Foundation Server STD ISO' do
		code  <<-EOH
			$volume = Mount-DiskImage -ImagePath "#{iso_path}/#{iso_filename}" -PassThru | Get-Volume
			Write-Host "XX$($volume.DriveLetter)XX"
			$volume.DriveLetter | Out-File "#{drive_letter_filepath}" -encoding ASCII
			EOH
		guard_interpreter :powershell_script
		not_if "($#{drive_letter} = (gwmi -Class Win32_LogicalDisk | Where-Object {$_.VolumeName -eq \"#{volume_name}\"}).VolumeName -eq \"#{volume_name}\")"
	end
	new_resource.updated_by_last_action(true)
end