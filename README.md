# HoloViewer

# Installation
0. [Download and install Unity LTS](https://unity3d.com/unity/qa/lts-releases). This was written using 2017.4.14f1.
1. Download and install Visual Studio Community.

# Start from this repo
0. Download or clone this repo.
1. Open Unity, and open the folder containing the repo as an existing Unity project.
2. Download and import the assets:
     1. Download the unitypackage of [HoloToolKit v2017.4.3.0](https://github.com/Microsoft/MixedRealityToolkit-Unity/releases) from the Microsoft Mixed Reality Toolkit repo.
     2. Import everything into your unity project with Assets -> Import Package -> Custom Package (navigate to the unitypackage you just downloaded). 
     3. Click "import all".
 3. Drag the assets to their respective slots in MyManager.
 4. Open Build Settings: File -> Build Settings.
 5. Add the HoloViewer scene to the build settings by clicking "Add open scenes".
 6. Select Universal Windows Platform and change the Target device to HoloLens.
 7. Select "Switch Platform" and wait while everything recompiles.
 7. Open Player Settings: Edit -> Project Settings -> Player. Under "Publishing settings" make sure "PrivateNetworkClientServer" is selected. Under XR Settings, make sure "Virtual Reality Supported" is selected, and Windows Mixed Reality is added.
 
 # Usage
 0. Make sure both ViveServer and HoloViewer will be running on the same computer network.
 0. Launch the ViveServer first. Make sure the controllers are being tracked on the Vive side by moving them around and/or clicking buttons.
 1. Launch on local machine: Hit the "play" button. 
 2. Launch on HoloLens with Holographic Emulator: Open the emulator by clicking Window -> Holographic Emulation. Now select the Emulation mode as "Remote to Device". On the HoloLens, launch the Holographic Emulation app. Type the IP address you see in the HoloLens into the IP address field in Unity: MyManager (Inspector), then edit the "Server IP" line. Clicking "Connect" in the Holographic Emulator window should make the floating IP address disappear from the HoloLens view. This means you're connected.
 3. Click "Play" in Unity to launch the app on the Hololens. 
 4. The first grip you click will define the right controller, then grip the left handle to define that one.
