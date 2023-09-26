## Pi Sliced Day Photos

This is a small program designed to be run on a Raspberry Pi that takes photographs at Sunrise, Sunset and at a number of specified intervals in-between.

The in-between intervals are calculated from the sunrise/sunset times and will not be at the same clock time each day - instead they will be at the same %/relative time thru the day or night. Probably the best example of 'why' is wanting a photo at sunrise, half way thru the day light and at sunset...

For this program to work it requires:
 - A settings file named PiSlicedDaySettings.json - an example is included in the code
 - A CSV file named SunriseAndSunset.csv with the calendar day, sunrise time (local) and sunset time (local)

In the settings file you can specify how many photographs you want taken between Sunrise and Sunset (during the day) and between Sunset and Sunrise (during the night) - zero is a valid setting. Sunrise and Sunset are always photographed.

The reason this program uses a file of Sunrise/Sunset times is to allow you to input any sunrise/sunset times that you want - a strong use case for this is generating topography compensated Sunrise/Sunset times for your location. For example today at my home we didn't see the sun come over the mountains to the east until 40+ minutes after the sun rise time calculated without the mountains in the way...

If you are interested in generated Topography compensated Sunrise/Sunset times for your location try:
  - [gvellut/tppss: Compute sunrise / sunset times taking into account local topography](https://github.com/gvellut/tppss) - this is a great free way to generate the times - it does take some setup, but like me you might find preparing the data for the program is an interesting learning project!
  - [Find Your Location and Compute Sunlight Conditions](https://www.suncurves.com/en/) - a paid service that will do this for you.
  - Various photography apps can calculate/show you this information but I'm not sure if any of them export yearly (or multi-year) data...

## Backstory and Setup

For a number of years my wife and I used a previous project - [cmiles/PiDropLapse](https://github.com/cmiles/PiDropLapse/tree/main) - and a [Raspberry Pi 4 Model B](https://www.raspberrypi.com/products/raspberry-pi-4-model-b/) to take periodic photographs and sensor readings to monitor an area inside our house. Since moving to a more rural property I have wanted to do a similar project but outside and solar powered - Raspberry Pi shortages, never quite finding an in-stock dedicated Pi solar setup that I loved and other house projects delayed that idea...

Recently we installed a 12V/200aH solar system near our parking area. The main purpose of this system is to power the rodent deterrent lights for our trucks - but it has more than enough power to also power a Pi for photo purposes!

### Equipment:
 - [Raspberry Pi 3 Model A+](https://www.raspberrypi.com/products/raspberry-pi-3-model-a-plus/) with [a case from Adafruit](https://www.adafruit.com/product/2359): This is about $32 USD plus shipping - I like both the $25 USD price of the 3 A+ and the selection of ports - more utility than the Zero 2 W and I wouldn't ever use all the ports on a 'full size' 3 or 4.
 - Wood and Plexiglass Enclosure: Hopefully weatherproof (enough)! Built with spare/scrap wood, left over plexiglass, bug screen sitting in a closet, caulk from working on floor transitions and the paint for the deck. The main feature is that I recycled existing materials for this! As you can see in the photos the carpentry is very basic and no details are included.
 - Solar: As mentioned above the solar system powering this setup wasn't designed primarily to run the Pi - for the sake of documenting the project the main components of the system are listed below. This is not a recommendation regarding anything below, I don't have enough experience to do that, and like most real world systems I had many constraints and goals that are not in line with 'build the world's best small solar system'. Details like wiring and fuses omitted.
	- 3x [Newpowa 100W 12V Mono Compact Solar Panels](https://www.newpowa.com/new-100w-compact-12v-mono-solar-panel/)
	- 2x [Ampere Time 12V 100Ah Lithium Batteries](https://www.amperetime.com/collections/ampere-time-12v-lithium-lifepo4-battery-series/products/ampere-time-12v-100ah-lithium-lifepo4-battery) - purchased used.
	- [Victron Energy SmartSolar MPPT 100/20](https://www.victronenergy.com/solar-charge-controllers/smartsolar-mppt-75-10-75-15-100-15-100-20)
	- [Victron Phoenix 12V/800W Inverter](https://www.victronenergy.com/inverters/phoenix-inverter-vedirect-250va-800va) with [Victron VE.Direct Bluetooth Smart Dongle](https://www.victronenergy.com/accessories/ve-direct-bluetooth-smart-dongle)
	- [Victron SmartShunt](https://www.victronenergy.com/battery-monitors/smart-battery-shunt)
	- [Raspberry Pi 3 Model A+](https://www.raspberrypi.com/products/raspberry-pi-3-model-a-plus/) running the [Victron Energy Venus OS](https://github.com/victronenergy/venus) to provide communication between the system and the [Victron Remote Monitoring System](https://www.victronenergy.com/panel-systems-remote-monitoring/vrm). See [Panbo's Raspberry Pi Victron Venus OS Install post](https://panbo.com/victrons-venus-os-on-a-raspberry-pi-install-and-configuration/) and as of 9/18/2023 see [Raspberry Pi 3A+: VRM Portal ID Missing](https://community.victronenergy.com/questions/79169/raspberry-pi-3a-vrm-id-missing.html) for critical information on getting Venus OS working correctly on the 3 A+. I used 2 [VE.Direct to USB interface cables](https://www.victronenergy.com/accessories/ve-direct-to-usb-interface) to connect the SmartSolar and SmartShunt to the Pi (currently the Bluetooth interface on the SmartSolar and SmartShunt is NOT used to connect to Victron Venus OS/Cerbo GX units! The Bluetooth does create a pretty great app experience, at least on Android...).

### Setup Notes

In the first version of my small wooden enclosure I tried to have the Pi take photographs thru a clear piece of plexiglass left over from another project (that was not focused on optical quality) - this did NOT work and in spite of playing with settings and trying both a Camera module 2 and 3 - I'm not currently sure if this is more related to the flatness or the optical quality of the plexiglass...

If you've worked in years gone by with the Pi Camera and C# you might have found the very useful [techyian/MMALSharp: C# wrapper to Broadcom's MMAL with an API to the Raspberry Pi camera.](https://github.com/techyian/MMALSharp) - without choosing an older version of Raspberry Pi OS that library doesn't work - the Pi has moved on to [libcamera](https://libcamera.org/). I didn't find a C# wrapper for libcamera and since I didn't need to do anything other than write stills to the Pi's storage simply calling libcamera-still 'command line style' seemed to be the best option.

I didn't find a single great place for libcamera-still documentation - frustrating until I figured out that (beyond 'getting started' content) running 'libcamera-still --help' was really the best starting spot.

## Other Projects

Fundamentally this project is just taking photographs with the Raspberry Pi which is not hard to do and there are free projects that do more than just take stills! One of my favorites is [GitHub - thomasjacquin's allsky: A Raspberry Pi operated Wireless Allsky Camera](https://github.com/thomasjacquin/allsky) - I hope to build on of these in the future...

## Tools and Libraries

This program would not be possible without the amazing resources available for creating Free software! Used in this project:

**Tools:**
  - [Visual Studio IDE](https://visualstudio.microsoft.com/), [.NET Core (Linux, macOS, and Windows)](https://dotnet.microsoft.com/download/dotnet-core)
  - [ReSharper: The Visual Studio Extension for .NET Developers by JetBrains](https://www.jetbrains.com/resharper/)
  - [GitHub Copilot · Your AI pair programmer · GitHub](https://github.com/features/copilot)
  - [AutoHotkey](https://www.autohotkey.com/)
  - [Compact-Log-Format-Viewer: A cross platform tool to read & query JSON aka CLEF log files created by Serilog](https://github.com/warrenbuckley/Compact-Log-Format-Viewer)
  - [Fork - a fast and friendly git client for Mac and Windows](https://git-fork.com/)
  - [LINQPad - The .NET Programmer's Playground](https://www.linqpad.net/)
  - [Notepad++](https://notepad-plus-plus.org/)

**Core Technologies:**
  - [dotnet/core: Home repository for .NET Core](https://github.com/dotnet/core)

**Libraries:**
  - [GitInfo | Git and SemVer Info from MSBuild, C# and VB](https://www.clarius.org/GitInfo/). MIT License.
  - [serilog/serilog: Simple .NET logging with fully-structured events](https://github.com/serilog/serilog). Easy full featured logging. Apache-2.0 License.
   - [RehanSaeed/Serilog.Exceptions: Log exception details and custom properties that are not output in Exception.ToString().](https://github.com/RehanSaeed/Serilog.Exceptions) MIT License.
   - [serilog/serilog-formatting-compact: Compact JSON event format for Serilog](https://github.com/serilog/serilog-formatting-compact). Apache-2.0 License.
   - [serilog/serilog-sinks-console: Write log events to System.Console as text or JSON, with ANSI theme support](https://github.com/serilog/serilog-sinks-console). Apache-2.0 License.
  - [toptensoftware/RichTextKit: Rich text rendering for SkiaSharp](https://github.com/toptensoftware/richtextkit). Apache-2.0 License.
  - [mono/SkiaSharp: SkiaSharp is a cross-platform 2D graphics API for .NET platforms based on Google's Skia Graphics Library. It provides a comprehensive 2D API that can be used across mobile, server and desktop models to render images.](https://github.com/mono/SkiaSharp). MIT License.
  - [NUnit.org](https://nunit.org/). [NUnit License](https://docs.nunit.org/articles/nunit/license.html)
  - [thomasgalliker/ObjectDumper: ObjectDumper is a utility which aims to serialize C# objects to string for debugging and logging purposes.](https://github.com/thomasgalliker/ObjectDumper). Apache-2.0 License.


