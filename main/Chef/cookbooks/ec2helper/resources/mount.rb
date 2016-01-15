property :volume_name, String
property :drive_letter, String

action :mount do
	powershell_script 'MountDrives' do
		code <<-EOH
			$offlineDisks = Get-Disk | Where OperationalStatus -eq 'Offline'
			$offlineDisks | ForEach-Object  {Set-Disk $_.Number -IsReadOnly $False}
			$offlineDisks | ForEach-Object {Set-Disk -Number $_.Number -IsOffline $False}
 			$volume = Get-WmiObject Win32_Volume -Filter "Label='#{volume_name}'"
			$volume.DriveLetter = drive_letter
			$volume.Put()
		EOH
		guard_interpreter :powershell_script
	end
	new_resource.updated_by_last_action(true)
end
