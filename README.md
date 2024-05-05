# LibJpeg-Turbo Unity

[libjpeg-turbo](https://github.com/libjpeg-turbo/libjpeg-turbo) for Unity.
libjpeg-turbo is a faster image codec. 

While Unity's [ImageConversion.EncodeArrayToJPG](https://docs.unity3d.com/ScriptReference/ImageConversion.EncodeArrayToJPG.html) is fast enough for single use, for every-frame use or on older PCs/Mobile platforms the difference is huge.

Currently supports Android armeabi-v7a/arm64-v8a and Windows x86/x64. At least that was my use case.

## Example/Demonstration
Captured on Pico Neo 3 Pro Eye, using the 3.0.2 release build. Numbers are averagaged milliseconds.


https://github.com/Tanshaydar/LibJpegTurboUnity/assets/1215477/d3e65f4b-fd1b-4049-b0a1-e2adae0ab277



## Unity project
Unity project is under UnityProject folder, and is a simple representation/demonstration of the usage. It also shows the folder structure and usage (i.e. create/import/dispose) of the library. It's using AsyncGPUReadback to capture the gameview and display it on the preview screens on top of the screen.

You can simply get the **Plugins.zip** from releases and put it on your project if you don't want to deal with it, but still, you may need the example.

## Library project
Library project is under CsProject folder and is used to create a dll file to use the libjpeg-turbo libraries.
Basically it includes an importer and encoder functionalities. 

You can simply get the .cs files from this project and put it under your Unity project to use it. You don't have to use the .dll file, but it's less clutter, and compatible with the IL2CPP build. 

## Building the actual libjpeg-turbo libraries from source

If you need to build the libraries yourself for any reason, or if an updated version of the libjpeg-turbo was released and you want to update, you need _Android NDK_, _Visual Studio_, _CMake_ and _make_ to be able to build the libraries yourself.

Go and get the source for libjpeg-turbo first.

### Android
Set the necessary variables to use the paths.
```
NDK_PATH=<your Android NDK root path>
TOOLCHAIN=$NDK_PATH/toolchains/llvm/prebuilt/windows-x86_64/
ANDROID_VERSION=29
```
Depending on your use case, you may change the Android version
#### armeabi-v7a
```
cmake -G "Unix Makefiles" -DANDROID_ABI=armeabi-v7a -DANDROID_ARM_MODE=arm -DANDROID_PLATFORM=android-${ANDROID_VERSION} -DANDROID_TOOLCHAIN=${TOOLCHAIN} -DCMAKE_ASM_FLAGS="--target=arm-linux-androideabi${ANDROID_VERSION}" -DCMAKE_TOOLCHAIN_FILE=${NDK_PATH}/build/cmake/android.toolchain.cmake -DCMAKE_MAKE_PROGRAM:FILEPATH=<your_make_binary_path> <your_libjpeg-turbo_source_folder>
```
Then simply
```
make
```

####arm64-v8a

```
cmake -G "Unix Makefiles" -DANDROID_ABI=arm64-v8a -DANDROID_ARM_MODE=arm -DANDROID_PLATFORM=android-${ANDROID_VERSION} -DANDROID_TOOLCHAIN=${TOOLCHAIN} -DCMAKE_ASM_FLAGS="--target=aarch64-linux-android${ANDROID_VERSION}" -DCMAKE_TOOLCHAIN_FILE=${NDK_PATH}/build/cmake/android.toolchain.cmake -DCMAKE_MAKE_PROGRAM:FILEPATH=<your_make_binary_path> <your_libjpeg-turbo_source_folder>
```
Then simply 
```
make
```

### Windows
#### x86
```
cmake -G "Visual Studio 17 2022" -A Win32 ../../../libjpeg-turbo-3.0.2/
```
#### x64
```
cmake -G "Visual Studio 17 2022" -A x64 ../../../libjpeg-turbo-3.0.2/
```

Then use the generated Visual Studio project files to build your own .dll file of the libjpeg-turbo. You can also use a different version of Visual Studio if you want, just go check the CMake documentation for it.

