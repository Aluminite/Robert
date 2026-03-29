# Robert, a Nintendo R.O.B. simulator
Robert is a software-based simulation of R.O.B. (Robotic Operating Buddy), an accessory released by Nintendo in 1985 for the Nintendo Entertainment System.
Robert has all of the functionality of a real R.O.B. unit and strives to follow it in functionality as accurately as possible.

Robert is compatible with and allows you to play both Gyromite and Stack-Up. It can connect either to a real NES through use of a special hardware device, or to the emulator Mesen.

# Screenshots
![The default view of the app](images/default.png)

![Stack-Up mode](images/stackup.png)

![Gyromite mode](images/gyromite.png)

# Usage
Download the Robert.zip file from the Releases page.

To use the main app, simply extract the files and run the .exe file. The app requires one of the two interfaces to be used:
### Emulator setup
Currently, only [Mesen](https://www.mesen.ca/) is supported. First, download and install it, and start running one of the games.

To connect to Robert, go to `Debug -> Scripts` to open the Script Window. 

Before running it for the first time, some settings must be changed. Select `Script -> Settings`, then in the setings window,
check both `Allow access to I/O and OS functions` and `Allow network access`. Click `OK`. This only needs to be done once.

Next, choose `File -> Open` and select the `robert-mesen.lua` file. You may now minimize the script window.

In the main Robert app, choose `Emulator` for the interface type then click `Connect`. The other settings only need to be changed if Mesen is running on a different system.

### Hardware interface setup
This requires a special device made out of a Raspberry Pi Pico microcontroller. A guide to make one will be coming soon.

---

This project was made without the use of any AI tools or assistance. All assets in this project have been made completely by me.