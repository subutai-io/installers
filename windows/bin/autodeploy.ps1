param(
    [string[]]$params = "",
    [string]$repoUrl,
    [string]$tomd5,
    [bool]$network_installation=$false,
    [string]$peer,
	[string[]] $services = "",
	[string[]] $remove = "",
    [string] $appdir
)

$CLONE="subutai-$(Get-date -format yyyyMMddhhmm)"

function set_variables{
	if (!($appdir -like "")){
		$env:Subutai = $appdir
		$env:Path = "$env:Path;$env:Subutai/bin;$env:SystemDrive\Program Files\Oracle\VirtualBox"
	}
}

function pause_here(){
	Write-Host "Press any key to continue ..."
	$x = $host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
}

function download_file($url, $destination){
    print_header_h1 "DOWNLOADING PREREQUISITES" $True
	$HTTP_Request = [System.Net.WebRequest]::Create($url)
	$HTTP_Status = $HTTP_Request.GetResponse().StatusCode

	If ($HTTP_Status -eq 200) { 
	    
	}
	Else {
    	Add-Type –AssemblyName System.Windows.Forms
		$oReturn=[System.Windows.Forms.MessageBox]::Show("Subutai repo is not available for some reason. Check your system and try running Subuta redeploy.","Repository Error",[System.Windows.Forms.MessageBoxButtons]::OK, [System.Windows.Forms.MessageBoxIcon]::ERROR)	
		switch ($oReturn){
			"OK" {
				#write-host "You pressed OK"
				# Enter some code
				create_deploy_shortcut
			}
		}
		exit 1
	}
	#$spWebApp = Get-SPWebApplication $url
	#$page = $SpSite.RootWeb.GetFile($resultsUrl);
	
	#if ($page.FileExists)
	#{
	#	Add-Type –AssemblyName System.Windows.Forms
	#	$oReturn=[System.Windows.Forms.MessageBox]::Show("Subutai repo is not available","Virtualization Extensions Error",[System.Windows.Forms.MessageBoxButtons]::OK, [System.Windows.Forms.MessageBoxIcon]::ERROR)	
	#	switch ($oReturn){
	#		"OK" {
	#			#write-host "You pressed OK"
	#			# Enter some code
	#		}
	#	}
	#	exit 1
	#}

    $start_time = Get-Date

    Import-Module BitsTransfer
    Start-BitsTransfer -Source $url -Destination $destination
    #OR
    #Start-BitsTransfer -Source $url -Destination -Asynchronous

    Write-Output "Time taken: $((Get-Date).Subtract($start_time).Seconds) second(s)"
}

function create_deploy_shortcut{
    $TARGET32 = "$env:Subutai\bin\deploy.bat"
    #$ShortcutFileStartMenu = "$env:SystemDrive\ProgramData\Microsoft\Windows\Start Menu\Programs\Oracle VM VirtualBox\Oracle VM VirtualBox.lnk"
    $ShortcutFileDesktop = "$env:Public\Desktop\Subutai-try-redeploy.lnk"

    create_shortcut $TARGET32 "" $ShortcutFileDesktop "" $False
}

function download_torrent($url, $destination){
    print_header_h1 "DOWNLOADING PREREQUISITES" $True

	cd $destination
    start_process aria "$url --seed-time=0 --dir=$destination"
}

function unzip_file($source, $dest){
    start_process 7z "x $source -o$dest -aoa"
}

function start_process($path, $arguments){
    Start-Process -FilePath $path -ArgumentList $arguments -Wait -NoNewWindow
}

function unzip_all($targetDir){
    print_header_h2 "EXTRACTING PREREQUISITES"

    foreach ($singleDirectory in (Get-ChildItem $targetDir -Recurse | Where-Object { $_.PSIsContainer }))
    {
        foreach($singleFile in Get-ChildItem $singleDirectory.FullName)
        {
            cd $singleDirectory.FullName

            # unzip archives
            if ($singleFile.Name -like "*.zip"){
                unzip_file $singleFile.Fullname $singleDirectory.Fullname
                rm $singleFile.Fullname
            }
        }
    }
}

function check_md5_all($targetDir){
    print_header_h2 "CHECKING MD5"

    foreach ($singleDirectory in (Get-ChildItem $targetDir -Recurse |Where-Object { $_.PSIsContainer } ))
    {
        foreach($singleFile in Get-ChildItem $singleDirectory.FullName)
        {
            cd $singleDirectory.FullName

            #check MD5
            if ($singleFile.Name.Contains(".md5")){
                $md5Report = (& md5sum -c $singleFile.Name) | Out-String
                echo "MD5 check for $md5Report"
                if ($md5Report -like "*FAILED*"){
                    cls
                    echo "MD5 check for $md5Report"
                    Write-Error "MD5 checksum did not match. Interrupting installation"
                    echo ""
                    echo "Don't worry, this message is yet for test purposes only"
                    pause_here
                } else{
                    rm $singleFile.Name
                }
            }
        }
    }
}

function network_installation{

    download_file $repoUrl "$env:Subutai/repomd5"

    print_header_h1 "EXTRACTING AND CHECKING MD5"

    cd $env:Subutai

    print_header_h2 "EXTRACTING REPO"
    unzip_file "$env:Subutai/repomd5" $env:Subutai

    rm "$env:Subutai/repomd5"

    check_md5_all $env:Subutai

    unzip_all $env:Subutai
}


function generate_md5{
    #cd $env:Subutai
    foreach ($singleDirectory in (Get-ChildItem $tomd5 -Recurse -Directory))
    {
        echo $singleDirectory.FullName
        cd $singleDirectory.FullName
        foreach($singleFile in Get-ChildItem $singleDirectory.FullName)
        {
            if((Get-Item $singleFile.FullName) -isnot [System.IO.DirectoryInfo]){
                start-process "cmd" "/c cd $($singleDirectory.Fullname) & C:\Users\won\Downloads\cygwin\bin\md5sum $($singleFile.Name) > $($singleFile.Name).md5"
            }
        }
    }
}

function print_header_h1($header, $shorten){
    cls
    if (!$shorten){
        ECHO ""
    }

    $header = " " + $header + " "
    $width = $(get-host).ui.rawui.windowsize.width - 1
    $content_width = $header.length
    $line = "#" * $width
    $prefix = "#" * (($width - $content_width) / 2)
    $postfix = "#" * ($width - $content_width - $prefix.length)
    $head = $prefix + $header + $postfix

    if (!$shorten) { Write-Host -f blue $line }
    Write-Host -f blue -NoNewLine $prefix
    Write-Host -f yellow -NoNewLine $header
    Write-Host -f blue $postfix
    if (!$shorten) { Write-Host -f blue $line }

}

function print_header_h2($header){
    ECHO ""
    Write-Host -f blue -NoNewLine " # "
    Write-Host -f yellow -NoNewLine "$header"
    Write-Host -f blue " #"
}

function deploy_redist{
    print_header_h2 "Deploying Tap driver"
    start_process $env:Subutai/redist/tap-driver.exe "/S"
    print_header_h2 "Deploying MS Visual C++"
    start_process $env:Subutai/redist/vcredist64.exe "/install /quiet"
    print_header_h2 "Deploying Chrome"
    Start-process -FilePath "$env:Subutai/redist/chrome.msi" /quiet -Wait
    #create_chrome_shortcut
    print_header_h2 "Deploying VirtualBox"
    start_process $env:Subutai/redist/virtualbox.exe "--silent"
    create_vbox_shortcut
}

function create_chrome_shortcut{
    $TARGET32 = "$env:ProgramFiles\Google\Chrome\Application\chrome.exe"
    $TARGET64 = "$env:ProgramFiles `(x86`)\Google\Chrome\Application\chrome.exe"
    $Arguments = "--load-extension=$env:SUBUTAI\extensions\pgp"
    $ShortcutFileStartMenu = "$env:SystemDrive\ProgramData\Microsoft\Windows\Start Menu\Programs\Google Chrome.lnk"
    $ShortcutFileDesktop = "$env:Public\Desktop\Google Chrome.lnk"

    create_shortcut $TARGET32 $TARGET64 $ShortcutFileStartMenu $Arguments $False
    create_shortcut $TARGET32 $TARGET64 $ShortcutFileDesktop $Arguments $False
}

function create_vbox_shortcut{
    $TARGET32 = "$env:SystemDrive\Program Files\Oracle\VirtualBox\VirtualBox.exe"
    $ShortcutFileStartMenu = "$env:SystemDrive\ProgramData\Microsoft\Windows\Start Menu\Programs\Oracle VM VirtualBox\Oracle VM VirtualBox.lnk"
    $ShortcutFileDesktop = "$env:Public\Desktop\Oracle VM VirtualBox.lnk"

    create_shortcut $TARGET32 "" $ShortcutFileStartMenu "" $True
    create_shortcut $TARGET32 "" $ShortcutFileDesktop "" $True
}

function create_shortcut($TARGET32, $TARGET64, $ShortcutFile, $Arguments, $Elevated){
    if(Test-Path $TARGET32){
        $TargetFile = $TARGET32
    } else{
        $TargetFile = $TARGET64
    }

    if(Test-Path $ShortcutFile){
        Remove-Item $ShortcutFile
    }
    $WScriptShell = New-Object -ComObject WScript.Shell
    $Shortcut = $WScriptShell.CreateShortcut($ShortcutFile)
    $Shortcut.TargetPath = $TargetFile
    $Shortcut.Arguments = $Arguments
    $Shortcut.Save()

    if($Elevated){
        $bytes = [System.IO.File]::ReadAllBytes("$ShortcutFile")
        $bytes[0x15] = $bytes[0x15] -bor 0x20 #set byte 21 (0x15) bit 6 (0x20) ON
        [System.IO.File]::WriteAllBytes("$ShortcutFile", $bytes)
    }
}

function restore_chrome_shortcut{
    $TARGET32 = "$env:ProgramFiles\Google\Chrome\Application\chrome.exe"
    $TARGET64 = "$env:ProgramFiles `(x86`)\Google\Chrome\Application\chrome.exe"
    $ShortcutFileStartMenu = "$env:SystemDrive\ProgramData\Microsoft\Windows\Start Menu\Programs\Google Chrome.lnk"
    $ShortcutFileDesktop = "$env:Public\Desktop\Google Chrome.lnk"

    create_shortcut $TARGET32 $TARGET64 $ShortcutFileStartMenu "" $False
    create_shortcut $TARGET32 $TARGET64 $ShortcutFileDesktop "" $False
}

function delete_p2p_services{
    $service = Get-WmiObject -Class Win32_Service -Filter "Name='Subutai Social P2P'"
    $service.delete()
    $service = Get-WmiObject -Class Win32_Service -Filter "Name='Subutai Social P2P Networking'"
    $service.delete()
}

function import_ovas{
    print_header_h2 "Importing OVA appliances"
    echo " -> Snappy OVA"
    vboxmanage import $env:Subutai\ova\snappy.ova
}

function prepare_nat_network {
    print_header_h2 "Preparing NAT network"
    vboxmanage natnetwork add --netname natnet1 --network "10.0.5.0/24" --enable --dhcp on
}

function clone_vm{
    print_header_h2 "Creating clone"
    vboxmanage clonevm --register --name $CLONE snappy
}

function prepare_nic {
    print_header_h2 "Restoring network"
    vboxmanage modifyvm $CLONE --nic4 none 2>&1>$null
    #& vboxmanage modifyvm $CLONE --natpf1 delete http-fwd 2>&1>$null
    #& vboxmanage modifyvm $CLONE --natpf1 delete https-fwd 2>&1>$null
    #& vboxmanage modifyvm $CLONE --natpf1 delete ssh 2>&1>$null
    vboxmanage modifyvm $CLONE --nic1 nat --cableconnected1 on  --natpf1 "ssh-fwd,tcp,,4567,,22" 2>&1>$null
    # vboxmanage modifyvm $CLONE --nic1 nat --cableconnected1 on  --natpf1 management-fwd,tcp,,11443,,8443 2>&1>$null
    vboxmanage modifyvm $CLONE --rtcuseutc on 2>&1>$null
    vboxmanage startvm --type headless $CLONE 2>&1>$null
}

function prepare_ssh_keys {
    print_header_h2 "Preparing SSH keys"
	md $env:Subutai/home/$env:username/.ssh
    New-Item $env:Subutai/home/$env:username/.ssh/authorized_keys -ItemType file
    New-Item $env:Subutai/home/$env:username/.ssh/known_hosts -ItemType file

	Start-process -FilePath "$env:Subutai/bin/ssh-keygen.exe" -ArgumentList "-q -t rsa -N '' -f '$env:Subutai/home/$env:username/.ssh/id_rsa'" -Wait
	
	pause
}

function deploy_templates {
    print_header_h2 "Cleaning keys"
	Start-process -FilePath "$env:Subutai/bin/ssh-keygen.exe" -ArgumentList "-f '$env:Subutai/home/$env:username/.ssh/known_hosts' -R [localhost]:4567" -Wait

    $code=$False

    $pubkey="$(cat $env:Subutai/home/$env:username/.ssh/id_rsa.pub)"

    print_header_h2 "Sending public RSA key"
    sshpass -p `"ubuntu`" ssh -o StrictHostKeyChecking=no ubuntu@localhost -p4567 "sudo bash -c echo $pubkey >> /root/.ssh/authorized_keys"

    print_header_h2 "Creating tmpfs"
    sshpass -p `"ubuntu`" ssh -o StrictHostKeyChecking=no ubuntu@localhost -p4567 "mkdir tmpfs; sudo mount -t tmpfs -o size=1G tmpfs /home/ubuntu/tmpfs"
    print_header_h2 "Copying snap"
    sshpass -p `"ubuntu`" scp -P4567 /redist/subutai/prepare-server.sh /redist/subutai/subutai_4.0.0_amd64.snap ubuntu@localhost:/home/ubuntu/tmpfs/
    $AUTOBUILD_IP="192.168.56.1"
    sshpass -p `"ubuntu`" ssh -o StrictHostKeyChecking=no ubuntu@localhost -p4567 "sed -i \`"s/IPPLACEHOLDER/$AUTOBUILD_IP/g\`" /home/ubuntu/tmpfs/prepare-server.sh"
    print_header_h2 "Running install script"
    sshpass -p `"ubuntu`" ssh -o StrictHostKeyChecking=no ubuntu@localhost -p4567 "sudo /home/ubuntu/tmpfs/prepare-server.sh"

    print_header_h2 "Copying templates"
    #sshpass -p `"ubuntu`" scp -P4567 /templates/master-subutai-template_4.0.0_amd64.tar.gz /templates/management-subutai-template_4.0.0_amd64.tar.gz ubuntu@localhost:/home/ubuntu/tmpfs/
    #sshpass -p `"ubuntu`" ssh -o StrictHostKeyChecking=no ubuntu@localhost -p4567 "sudo mv /home/ubuntu/tmpfs/master-subutai-template_4.0.0_amd64.tar.gz /mnt/lib/lxc/lxc-data/tmpdir/"
    #sshpass -p `"ubuntu`" ssh -o StrictHostKeyChecking=no ubuntu@localhost -p4567 "sudo mv /home/ubuntu/tmpfs/management-subutai-template_4.0.0_amd64.tar.gz /mnt/lib/lxc/lxc-data/tmpdir/"

    #echo "Peer parameter = $peer"
    #echo "Network installation parameter = $network_installation"

    if(!($peer -like "rh-only")){
        #echo "OK, it's not rh-only installation"
        if($peer -like "trial"){
            #echo "Seems like it is trial peer"
            if(!$network_installation){
                #echo "But not network installation"
                print_header_h2 "Setting iptables rules"
				sshpass -p `"ubuntu`" ssh -o StrictHostKeyChecking=no ubuntu@localhost -p4567 "sudo iptables -P INPUT DROP; sudo iptables -P OUTPUT DROP; sudo iptables -A INPUT -p tcp -m tcp --dport 22 -j ACCEPT; sudo iptables -A OUTPUT -p tcp --sport 22 -m state --state ESTABLISHED,RELATED -j ACCEPT"
            }

            print_header_h2 "Installing master template"
            if($network_installation){
                sshpass -p `"ubuntu`" ssh -o StrictHostKeyChecking=no ubuntu@localhost -p4567 "sudo subutai -d import master"
            } else {
                sshpass -p "ubuntu" ssh -o StrictHostKeyChecking=no ubuntu@localhost -p4567 "sudo echo -e "y" | sudo subutai import master"
            }

            print_header_h2 "Installing management template"
            if($network_installation){
                sshpass -p `"ubuntu`" ssh -o StrictHostKeyChecking=no ubuntu@localhost -p4567 "sudo subutai -d import management"
            } else {
                sshpass -p `"ubuntu`" ssh -o StrictHostKeyChecking=no ubuntu@localhost -p4567 "sudo echo -e "y" | sudo subutai import management"
            }

            if(!$network_installation){
                print_header_h2 "Setting iptables rules"
                sshpass -p `"ubuntu`" ssh -o StrictHostKeyChecking=no ubuntu@localhost -p4567 "sudo iptables -P INPUT ACCEPT; sudo iptables -P OUTPUT ACCEPT"
            }
        }
    }

    print_header_h2 "Removing VirtualBox SSH forwarding rule"
    vboxmanage modifyvm $CLONE --natpf1 delete "ssh-fwd" 2>&1>$null
}

function deploy_p2p_service ($serviceName, $binaryPath, $login, $pass)
{
    $login = "NT AUTHORITY\NETWORK SERVICE"
    $psw = "yo"

    $scuritypsw = ConvertTo-SecureString $psw -AsPlainText -Force

    $mycreds = New-Object System.Management.Automation.PSCredential($login, $scuritypsw)

    $serviceName1 = "Subutai Social P2P"
    $binaryPath1 = "$env:Subutai\bin\p2p.exe daemon"

    $serviceName2 = "Subutai Social P2P Networking"
    $binaryPath2 = "$env:Subutai\bin\p2p.exe start -ip 10.10.10.1 -hash UNIQUE_STRING_IDENTIFIER"

    New-Service -name $serviceName1 -binaryPathName $binaryPath1 -displayName $serviceName1 -startupType Automatic -credential $mycreds
    New-Service -name $serviceName2 -binaryPathName $binaryPath2 -displayName $serviceName2 -startupType Automatic -credential $mycreds
}

function deploy_p2p_service_nssm ()
{
    $serviceName = "Subutai Social P2P"
    $binaryPath = "$env:Subutai\bin\p2p.exe"
    $arguments = "daemon"

    deploy_service_nssm $serviceName $binaryPath $arguments
}

function remove_p2p_service_nssm(){
    remove_service_nssm "Subutai Social P2P"
}

function deploy_service_nssm($serviceName, $binaryPath, $arguments){
    cd $env:Subutai
    nssm install $serviceName $binaryPath $arguments
    nssm start $serviceName
}

function remove_service_nssm($serviceName){
	stop-service "Subutai Social P2P"
    nssm remove $serviceName confirm
}

function remove_subutai_vms(){
    $vms = $(vboxmanage list vms)
    foreach($vm in $vms.Split(" ")){
        if($vm -like '"snappy"' -Or $vm -like '"subutai-*"'){
            remove_vm($vm)
            echo $vm
        }
    }
}

function remove_vm($name){
    vboxmanage controlvm $name poweroff
    vboxmanage unregistervm $name --delete
}

function clean_after_uninstall(){
    restore_chrome_shortcut
    remove_subutai_vms
}


if ($params.length -eq 0){
    echo "Hey, we need some parameters here"
} else{

	set_variables

    if ($network_installation){
        network_installation
		
    }
    if ($params -contains "deploy-redist"){
		print_header_h1 "DEPLOYING REDISTRIBUTABLES"
        deploy_redist
		
    }
    if ($params -contains "prepare-nat"){
		print_header_h1 "PREPARING VIRTUAL BOX"
        prepare_nat_network
    }
    if ($params -contains "import-ovas"){
        import_ovas
    }

	if ($params -contains "prepare-ssh"){
		print_header_h1 "CREATING SSH KEYS"
        prepare_ssh_keys
    }

    if ($params -contains "clone-vm"){
		print_header_h1 "SETTING UP VBOX CONTAINER"
        clone_vm
        prepare_nic
    }
    if ($params -contains "deploy-templates"){
        deploy_templates
    }

    if ($services -contains "p2p"){
		print_header_h1 "SETTING UP SERVICES"
        deploy_p2p_service_nssm
    }

    if ($params -contains "clean-after-uninstall"){
        clean_after_uninstall
    }

    if($params -contains "generate-md5"){
        generate_md5
    }

    if($params -contains "wait"){
        print_header_h1 "SUBUTAI HAS BEEN SUCCESSFULLY INSTALLED"
        echo ""
        pause_here
		Start-process -FilePath "$env:Subutai/bin/tray/SubutaiTray.exe"
    }
}