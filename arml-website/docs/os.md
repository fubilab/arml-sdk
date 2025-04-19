# ARML Operating System guide

The mainboard of the ARML (LattePanda Delta 3 864) has support for Ubuntu 20.04 LTS and 22.04 LTS. The ARML does not require any custom build or installation of the OS, so you can follow the [installation instructions from LattePanda](https://docs.lattepanda.com/content/3rd_delta_edition/Operating_Systems_Ubuntu/) if you want to install or reinstall the OS on the ARML.

## Configuration

There is some basic configuration necessary to prepare the OS on the ARML, such as installing the launcher application and making it accessible as a network drive for deploying applications. We recommend that you follow all of the steps below.

### Install the [launcher application](./launcher.md)

1. Download the [latest release of the launcher application](https://github.com/fubilab/arml-sdk/releases/latest/download/applauncher.zip). 
2. Create a directory on the desktop called `unitybuilds`.
3. Extract the contents of the downloaded zip file to the `unitybuilds` directory. If all goes well, you should have a directory named `_applauncher` inside `unitybuilds`.
4. Run the _Startup Applications_ app from the Ubuntu launcher (usually accessed by pressing the Windows or Apple key on your keyboard). 
5. Click "Add" and then "Browse". Browse to `Desktop -> unitybuilds -> _applauncher -> linux_applauncher.x86_64`
6. Click "Save"

Now, when you reboot, the launcher app should appear automatically.

### Hide the bootloader menu

To prevent the GRUB bootloader menu from showing, run the following

```bash
sudo gedit /etc/default/grub
```

Then delete everything in the file and enter the following:

```
GRUB_DEFAULT=0
GRUB_TIMEOUT_STYLE=hidden
GRUB_HIDDEN_TIMEOUT=0
GRUB_HIDDEN_TIMEOUT_QUIET=true
GRUB_TIMEOUT=0
GRUB_RECORDFAIL_TIMEOUT=0
GRUB_CMDLINE_LINUX_DEFAULT="fsck.mode=skip quiet splash"
```

Save the file, then run:

```bash
sudo update-grub
```

Now, when you restart, you should not see the bootloader menu.

### Audio configuration

1. Run the following commands:

    ```bash
    amixer set 'Auto-Mute Mode' Disabled
    amixer set Speaker 50% toggle
    sudo alsactl store
    ```
    
2. Install pavucontrol:

    ```
    sudo apt install pavucontrol
    ```

3. Run `pavucontrol` and in the **Configuration** tab, set **Built-in Audio** profile to **Analog Stereo Duplex** and set all other profiles to **Off.**

4. Run `sudo gedit /etc/pulse/default.pa`, add the following lines at the bottom, and save the file: 
    
    ```bash
    ### Make some devices default
    set-default-sink alsa_output.pci-0000_00_1f.3.analog-stereo
    set-default-source alsa_input.pci-0000_00_1f.3.analog-stereo
    set-sink-port alsa_output.pci-0000_00_1f.3.analog-stereo analog-output-speaker
    set-source-port alsa_input.pci-0000_00_1f.3.analog-stereo analog-input-mic
    ```

### Install Brave (or other browser)
The next step (Disable software update notifications) will remove the pre-installed software, including Firefox, so it is recommended to install another browser before proceeding.

Run the following:
```bash
sudo apt install curl
curl -fsS https://dl.brave.com/install.sh | sh
```

### Disable software update notifications
1. In Settings > Updates > Automatically check for updates, select "Never"
2. In Settings > Updates > Notify me of a new Ubuntu version, select "Never"
2. Run the following:
```bash
gsettings set com.ubuntu.update-notifier no-show-notifications true
sudo apt remove update-notifier update-notifier-common
sudo apt remove --purge gnome-software
sudo apt autoremove --purge snapd
```

### Create network share
If you want to access the ARML builds directory over the network, you need to add an SMB network share to the `unitybuilds` directory you created in the [Install the launcher app](./os.md#install-the-launcher-application) step. 

This assumes your user is `goblin`, so adapt the username for your system.  

1. Install samba by running:
    ```bash
    sudo apt install samba
    ```

2. Open `/etc/samba/smb.conf` as root and add the following lines. This assumes your username is `goblin`:
    ```bash
    [unitybuilds]
    comment = Unity builds
    path = /home/goblin/Desktop/unitybuilds
    browseable = yes
    guest ok = no
    read only = no
    force user = goblin
    force group = goblin
    valid users = goblin
    ```

3. Set the password for the SAMBA user. Note that we can't use guest access in Windows 11+, so we have to specifiy a user and password.
    ```bash
    sudo smbpasswd -a goblin
    ```

4. Set permissions to allow network share users to write to `unitybuilds`:
    ```bash
    sudo chmod -R a+x /home
    sudo chmod -R a+w /home/goblin/Desktop/unitybuilds
    ```

5. Restart the samba service:
    ```bash
    sudo service smbd restart
    ```

### Configure the USB Ethernet
If you want to connect the ARML to another computer using ethernet to be able to quickly deploy builds, do the following:
1. Connect a USB-C hub that has an Ethernet port to the ARML. See the USBHUB entry in [Peripherals](./peripherals.md) for recommended hardware.
2. Power on the ARML using one of the options in [Powering the ARML](./power.md#powering-the-ar-magic-lantern).
3. Use an ethernet cable to connect the USB hub from (1) to an ethernet port on your computer. If your computer doesn't have an ethernet port, or it is already occupied, use an ethernet-to-USB adaptor to connect the ethernet to an available USB port on your computer. See the USBNET entry in [Peripherals](./peripherals.md) for recommended hardware.
4. Configure the network adapter on your computer (either the builtin or the USB adaptor) to use manual IP address assignment (not DHCP) with the following settings:
   - IP address: 192.168.121.2
   - Netmask: 255.255.255.0 (24)
   - Gateway: 192.168.121.1
5. Configure the network adaptor on the ARML. The following directions are for Ubuntu 20.04, and may differ slightly if you are running a different OS on the ARML. You will need to connect a monitor, mouse and keyboard to the USB hub attached to the ARML for this step.
   - In the Ubuntu launcher, type "network" and open "Network"
   - Select the network adapter that corresponds to the USB hub (it is probably called "USB Ethernet")
   - Click the gear icon in the lower right to configure the adaptor.
   - Under IPv4, choose "Manual" method and enter the following settings and click "Apply":
      - IP address: 192.168.121.1
      - Netmask: 255.255.255.0
      - Gateway: 192.168.121.1
6. If everything went well, you should be able to enter `\\192.168.121.1` in the file explorer on your computer, enter the username and password configured [above](#create-network-share), and see the `unitybuilds` shared directory. If this method fails, try the [Alternative method for deploying to the ARML](./workflow.md#alternative-method-for-deploying-to-the-arml).

### Install the Arduino software and scripts
1. Install AppImageLauncher
    ```bash
    sudo apt install software-properties-common
    sudo add-apt-repository ppa:appimagelauncher-team/stable
    sudo apt update
    sudo apt install appimagelauncher
    ```
2. Add serial port access to your user. Change `goblin` to your username:
   ```
   sudo usermod -aG dialout goblin
   sudo usermod -aG tty goblin
   ```
3. Restart the system
4. Download the AppImage 64 bits (X86-64) from the [Arduino Software page](https://www.arduino.cc/en/software).
5. [Change the permissions on the AppImage file](https://docs.arduino.cc/software/ide-v2/tutorials/getting-started/ide-v2-downloading-and-installing/#linux) that was downloaded to allow execution.
6. Double-click the AppImage to run the Arduino IDE. Follow the AppImageLauncher instructions to install the IDE to the Ubuntu Applications directory.
7. Go to `Tools > Manage Libraries...` menu and add the following libraries:
    - Adafruit BNO055 (and all dependencies)
    - Adafruit NeoPixel
8. Download the Arduino script from the [ARML SDK](https://github.com/fubilab/arml-sdk): https://github.com/fubilab/arml-sdk/blob/main/arml-arduino/arml-arduino.ino. It must be placed in a directory called `arml-arduino`.
9. Open `arml-arduino.ino` in the Arduino IDE.
10. In the "Select Board" menu, choose the first item ("Unknown") and set the board to "Arduino Leonardo"
11. Click the "Upload" button to program the microcontroller on the ARML.

### Install dependencies

The SDK depends on some system libraries to run properly. Most are installed automatically by Ubuntu, but there is one that we have found to be missing on Ubuntu Jammy 22.04. 

To install the missing dependency, run:
```bash
sudo apt install libusb-1.0-0-dev
```

### Add udev rules

The [camera SDK ](https://docs.luxonis.com) requires permission to access the USB ports. Run the following:

```bash
echo 'SUBSYSTEM=="usb", ATTRS{idVendor}=="03e7", MODE="0666"' | sudo tee /etc/udev/rules.d/80-movidius.rules
sudo udevadm control --reload-rules && sudo udevadm trigger
```

## Troubleshooting

When running a Unity application built with the SDK, you can debug problems by showing the system log onscreen. Press the _menu_ button on the remote (or, if you are on desktop, some keyboards have a menu button) and click "Toggle Log".

### Tracking not working

If you run one of the SDK sample apps _HelloWorld_ or [WallGame](./wallgame.md) and the tracking isn't working at all, check the system log. If there is an error message that starts with "DllNotFoundException", make sure you have followed [Install dependencies](#install-dependencies) above. If you have and you are still getting that error, check for other missing dependencies by running the commands below.

This assumes your username is `goblin` and you have the _HelloWorld_ app installed on the ARML:
```bash
ldd /home/goblin/Desktop/unitybuilds/HelloWorld/HelloWorld_Data/Plugins/libspectacularAI_unity.so 
```

If any of the libraries show "not found" after their name, find instructions online for how to install it or contact us on the [Discord](https://discord.gg/zWZT3yKf4q) for assistance.