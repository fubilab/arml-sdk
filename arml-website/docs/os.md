# ARML Operating System guide

The mainboard of the ARML (LattePanda Delta 3 864) has support for Ubuntu 20.04 LTS and 22.04 LTS. The ARML does not require any custom build or installation of the OS, so you can follow the [installation instructions from LattePanda](https://docs.lattepanda.com/content/3rd_delta_edition/Operating_Systems_Ubuntu/) if you want to install or reinstall the OS on the ARML.

## Configuration

There is some basic configuration necessary to prepare the OS on the ARML for optimal functioning and make it accessible as a network drive for deploying applications.

### Audio configuration

1. Run the following commands:

    ```bash
    amixer set 'Auto-Mute Mode' Disabled
    amixer set Speaker 50% toggle
    sudo alsactl store
    ```
    
2. Run `pavucontrol` and in the **Configuration** tab, set **Built-in Audio** profile to **Analog Stereo Duplex** and set all other profiles to **Off.**

3. Run `sudo pico /etc/pulse/default.pa` and add the following lines at the bottom: 
    
    ```bash
    ### Make some devices default
    set-default-sink alsa_output.pci-0000_00_1f.3.analog-stereo
    set-default-source alsa_input.pci-0000_00_1f.3.analog-stereo
    set-sink-port alsa_output.pci-0000_00_1f.3.analog-stereo analog-output-speaker
    set-source-port alsa_input.pci-0000_00_1f.3.analog-stereo analog-input-mic
    ```
### Disable software update notifications
Run the following:
```bash
gsettings set com.ubuntu.update-notifier no-show-notifications true
sudo apt remove update-notifier update-notifier-common
sudo apt remove --purge gnome-software
sudo apt autoremove --purge snapd
```

### Create build directory and add network share
If you want to access the ARML builds directory over the network, do the following:
1. Create a directory on the desktop called `unitybuilds`.
2. Add an SMB network share. This assumes your user is `fubintlab`, so adapt the username for your system.  
    Open `/etc/samba/smb.conf` as root and add the following lines:
    ```bash
    [unitybuilds]
    comment = Unity builds
    path = /home/fubintlab/Desktop/unitybuilds
    browseable = yes
    guest ok = yes
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
   - Under IPv4, choose "Manual" method and enter the following settings:
      - IP address: 192.168.121.1
      - Netmask: 255.255.255.0
      - Gateway: 192.168.121.1
6. If everything went well, you should be able to enter `\\192.168.121.1` in the file explorer on your computer and see the "unitybuilds" shared directory. If this method fails, see the following section for an alternative method.

### Install the Arduino software and scripts
1. Follow the directions from Arduino to [install the Arduino IDE](https://docs.arduino.cc/software/ide-v1/tutorials/Linux/).
2. Download the Arduino script from the [ARML SDK](https://github.com/fubilab/arml-sdk): https://github.com/fubilab/arml-sdk/blob/main/arml-arduino/arml-arduino.ino. It must be placed in a directory called `arml-arduino`.
3. Open `arml-arduino.ino` in the Arduino IDE and click the "Upload" button to program the microcontroller on the ARML.