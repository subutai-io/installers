param(
    [string[]]$params,
    [string]$repoUrl,
    [string]$tomd5,
    [bool]$network_installation=$false,
    [string]$peer="rh-only"
)

$CLONE="subutai-$(Get-date -format yyyyMMddhhmm)"

function network_installation{
    $env:Path="$env:Path;$env:Subutai\bin"

    cd $env:Subutai
    ./bin/wget -q --show-progress -r -nH --cut-dirs=1 --no-parent --reject "index.html*" $repoUrl
    $sharepath = $env:Subutai
    $Acl = Get-ACL $SharePath
    $AccessRule= New-Object System.Security.AccessControl.FileSystemAccessRule("everyone","full","ContainerInherit,Objectinherit","none","Allow")
    $Acl.AddAccessRule($AccessRule)
    Set-Acl $SharePath $Acl

    foreach ($singleDirectory in (Get-ChildItem $env:Subutai -Recurse -Directory))
    {
        Set-Acl $singleDirectory.FullName $Acl

        foreach($singleFile in Get-ChildItem $singleDirectory.FullName)
        {
            Set-Acl $singleFile.FullName $Acl
            cd $singleDirectory.FullName

            #check MD5
            if ($singleFile.Name.Contains(".md5")){
                $md5Report = $(md5sum -c $singleFile.Name)
                echo "MD5 check for $md5Report"
            }

            # unzip archives
            if ($singleFile.Name.Contains(".zip")){
                Add-Type -A System.IO.Compression.FileSystem
                [IO.Compression.ZipFile]::ExtractToDirectory($singleFile.FullName, $singleDirectory.FullName) 2>&1>$null
            }
        }
    }
}

function check_md5($fullName, $name, $md5){
    $md5Out = $(.\bin\md5sums $fullName -b -u)
    $fMd5 = $md5Out -split ' '
    if ($fMd5[0] -eq $md5){
        echo "MD5 checksum for '$name' \t\t OK"
    } else{
        echo "MD5 checksum for '$name' \t\t FAIL"
    }
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

function deploy_redist{
    echo "Deploying Tap driver"
    Start-process -FilePath "$env:Subutai/redist/tap-driver.exe" -ArgumentList "/S" -Wait
    echo "Deploying MS Visual C++"
    Start-process -FilePath "$env:Subutai/redist/vcredist64.exe" -ArgumentList "/install /quiet" -Wait
    echo "Deploying Chrome"
    Start-process -FilePath "$env:Subutai/redist/chrome.msi" /quiet -Wait
    create_chrome_shortcut
    echo "Deploying VirtualBox"
    Start-process -FilePath "$env:Subutai/redist/virtualbox.exe" -ArgumentList "--silent" -Wait
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
    echo "Importing OVA appliances"
    echo " -> Snappy OVA"
    vboxmanage import $env:Subutai\ova\snappy.ova
}

function prepare_nat_network {
    echo "Preparing NAT network"
    vboxmanage natnetwork add --netname natnet1 --network "10.0.5.0/24" --enable --dhcp on
}

function clone_vm{
    echo "Creating clone"
    vboxmanage clonevm --register --name $CLONE snappy
}

function prepare_nic {
    echo "Restoring network"
    vboxmanage modifyvm $CLONE --nic4 none
    vboxmanage modifyvm $CLONE --natpf1 delete http-fwd
    vboxmanage modifyvm $CLONE --natpf1 delete https-fwd
    vboxmanage modifyvm $CLONE --natpf1 delete ssh
    vboxmanage modifyvm $CLONE --nic1 nat --cableconnected1 on  --natpf1 "ssh-fwd,tcp,,4567,,22"
    vboxmanage modifyvm $CLONE --nic1 nat --cableconnected1 on  --natpf1 "management-fwd,tcp,,11443,,8443"
    vboxmanage modifyvm $CLONE --rtcuseutc on
    vboxmanage startvm --type headless $CLONE
}

function prepare_ssh_keys {
    echo "Preparing SSH keys"
    New-Item $env:Subutai/home/user/.ssh/authorized_keys -ItemType file
    New-Item $env:Subutai/home/user/.ssh/known_hosts -ItemType file

    Start-process -FilePath "$env:Subutai/bin/ssh-keygen.exe" -ArgumentList "-q -t rsa -N '' -f $env:Subutai/home/user/.ssh/id_rsa" -Wait
}

function deploy_templates {
    echo "Cleaning keys"
    Start-process -FilePath "$env:Subutai/bin/ssh-keygen.exe" -ArgumentList "-f $env:Subutai/home/user/.ssh/known_hosts -R [localhost]:4567" -Wait

    echo "Subutai path is $env:Subutai/bin/"
    cd $env:Subutai/bin
    $code=$False

    $pubkey="$(cat $env:Subutai/home/user/.ssh/id_rsa.pub)"


    echo "Waiting for SSH session"
    while(!($code)){
        ./sshpass -p `"ubuntu`" ./ssh -o ConnectTimeout=1 -o StrictHostKeyChecking=no ubuntu@localhost -p4567 `"ls`" >$null 2>&1
        $code = $?
        Start-Sleep -s 2
    }

    echo "Sending public RSA key"
    ./sshpass -p `"ubuntu`" ./ssh -o StrictHostKeyChecking=no ubuntu@localhost -p4567 "sudo bash -c 'echo $pubkey >> /root/.ssh/authorized_keys"

    echo "Creating tmpfs"
    ./sshpass -p `"ubuntu`" ./ssh -o StrictHostKeyChecking=no ubuntu@localhost -p4567 "mkdir tmpfs; sudo mount -t tmpfs -o size=1G tmpfs /home/ubuntu/tmpfs"
    echo "Copying snap"
    ./sshpass -p `"ubuntu`" ./scp -P4567 /redist/subutai/prepare-server.sh /redist/subutai/subutai_4.0.0_amd64.snap ubuntu@localhost:/home/ubuntu/tmpfs/
    $AUTOBUILD_IP=192.168.56.1
    ./sshpass -p `"ubuntu`" ./ssh -o StrictHostKeyChecking=no ubuntu@localhost -p4567 "sed -i \`"s/IPPLACEHOLDER/$AUTOBUILD_IP/g\`" /home/ubuntu/tmpfs/prepare-server.sh"
    echo "Running install script"
    ./sshpass -p `"ubuntu`" ./ssh -o StrictHostKeyChecking=no ubuntu@localhost -p4567 "sudo /home/ubuntu/tmpfs/prepare-server.sh"

    echo "Copying templates"
    #./sshpass -p `"ubuntu`" ./scp -P4567 /templates/master-subutai-template_4.0.0_amd64.tar.gz /templates/management-subutai-template_4.0.0_amd64.tar.gz ubuntu@localhost:/home/ubuntu/tmpfs/
    #./sshpass -p `"ubuntu`" ./ssh -o StrictHostKeyChecking=no ubuntu@localhost -p4567 "sudo mv /home/ubuntu/tmpfs/master-subutai-template_4.0.0_amd64.tar.gz /mnt/lib/lxc/lxc-data/tmpdir/"
    #./sshpass -p `"ubuntu`" ./ssh -o StrictHostKeyChecking=no ubuntu@localhost -p4567 "sudo mv /home/ubuntu/tmpfs/management-subutai-template_4.0.0_amd64.tar.gz /mnt/lib/lxc/lxc-data/tmpdir/"


    if(!$peer -like "rh-only"){

        if($peer -like "trial"){
            if(!network_installation){
                echo "Setting iptables rules"
                ./sshpass -p `"ubuntu`" ./ssh -o StrictHostKeyChecking=no ubuntu@localhost -p4567 "sudo iptables -P INPUT DROP; sudo iptables -P OUTPUT DROP; sudo iptables -A INPUT -p tcp -m tcp --dport 22 -j ACCEPT; sudo iptables -A OUTPUT -p tcp --sport 22 -m state --state ESTABLISHED,RELATED -j ACCEPT"
            }

            echo "Installing master template"
            if(network_installation){
                ./sshpass -p `"ubuntu`" ./ssh -o StrictHostKeyChecking=no ubuntu@localhost -p4567 "sudo subutai -d import master"
            } else {
                ./sshpass -p `"ubuntu`" ./ssh -o StrictHostKeyChecking=no ubuntu@localhost -p4567 "sudo echo -e 'y' | sudo subutai import master"
            }

            echo "Installing management template"
            if(network_installation){
                ./sshpass -p `"ubuntu`" ./ssh -o StrictHostKeyChecking=no ubuntu@localhost -p4567 "sudo subutai -d import management"
            } else {
                ./sshpass -p `"ubuntu`" ./ssh -o StrictHostKeyChecking=no ubuntu@localhost -p4567 "sudo echo -e 'y' | sudo subutai import management"
            }

            if(!network_installation){
                echo "Setting iptables rules"
                ./sshpass -p `"ubuntu`" ./ssh -o StrictHostKeyChecking=no ubuntu@localhost -p4567 "sudo iptables -P INPUT ACCEPT; sudo iptables -P OUTPUT ACCEPT"
            }
        }
    }

    echo "Removing VirtualBox SSH forwarding rule"
    vboxmanage modifyvm $CLONE --natpf1 delete "ssh-fwd"
    pause
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
    ./bin/nssm install $serviceName $binaryPath $arguments
    ./bin/nssm start $serviceName
}

function remove_service_nssm($serviceName){
    cd $env:Subutai
    ./nssm remove $serviceName confirm
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
    remove_p2p_service_nssm
    Remove-Item $env:SystemDrive\Subutai\home -Force -Recurse
    remove_subutai_vms
}

if ($params.length -eq 0){
    echo "Hey, we need some parameters here"
} else{

    if ($network_installation){
        network_installation
    }
    if ($params.Contains("deploy-redist")){
        deploy_redist
    }
    if ($params.Contains("prepare-nat")){
        prepare_nat_network
    }
    if ($params.Contains("import-ovas")){
        import_ovas
    }
    if ($params.Contains("clone-vm")){
        clone_vm
        prepare_nic
    }
    if ($params.Contains("deploy-p2p")){
        deploy_p2p_service_nssm
    }
    if ($params.Contains("prepare-ssh")){
        prepare_ssh_keys
    }
    if ($params.Contains("deploy-templates")){
        deploy_templates
    }
    if ($params.Contains("uninstall")){
        $app = Get-WmiObject -Class Win32_Product -Filter "Name = 'Subutai'"
        $app.uninstall()
    }
    if ($params.Contains("clean-after-uninstall")){
        clean_after_uninstall
    }
    if ($params.Contains("delete-p2p")){
        delete_p2p_services
    }
    if($params.Contains("generate-md5")){
        generate_md5
    }
}