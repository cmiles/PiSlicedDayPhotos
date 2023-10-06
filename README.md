## Pi Sliced Day Photos

This is a small program designed to be run on a Raspberry Pi that takes photographs at Sunrise, Sunset, a number of specified intervals in-between and at custom times either specified by clock time or minutes +/- from sunrise/sunset.

The in-between intervals are calculated from the sunrise/sunset times and will not necessarily be at the same clock time each day - instead they will be at the same %/relative time thru the day or night. For me, living below mountian peaks to the east, the amount of daylight/darkness at my house has become a frame for my life and makes more intuitive sense to me than clock time or absolute position of the sun, so a system that take a photograph 'half way thru the amount of the day when the sun is visible' is personally meaningful.

For this program to work it requires:
 - A settings file named PiSlicedDaySettings.json - an example is included in the code
 - A CSV file named SunriseAndSunset.csv with the calendar day, sunrise time (local) and sunset time (local)

Sunrise and Sunset are always photographed. In the settings file you can specify how many photographs you want taken between Sunrise and Sunset (during the day - 0 is valid), between Sunset and Sunrise (during the night - 0 is valid) and specify custom times either as clock times or as Sunrise/Sunset +/- minutes.

The reason this program uses a file of Sunrise/Sunset times is to allow you to input any sunrise/sunset times that you want - a strong use case for this is generating topography compensated Sunrise/Sunset times for your location. For example today at my home we didn't see the sun come over the mountains to the east until 40+ minutes after the sun rise time calculated without the mountains in the way...

If you are interested in generated Topography compensated Sunrise/Sunset times for your location try:
  - [gvellut/tppss: Compute sunrise / sunset times taking into account local topography](https://github.com/gvellut/tppss) - this is a great free way to generate the times - it does take some setup, but like me you might find preparing the data for the program is an interesting learning project!
  - [Find Your Location and Compute Sunlight Conditions](https://www.suncurves.com/en/) - a paid service that will do this for you.
  - Various photography apps can calculate/show you this information but I'm not sure if any of them export yearly (or multi-year) data...

## Backstory and Setup

For a number of years my wife and I used a previous project - [cmiles/PiDropLapse](https://github.com/cmiles/PiDropLapse/tree/main) - and a [Raspberry Pi 4 Model B](https://www.raspberrypi.com/products/raspberry-pi-4-model-b/) to take periodic photographs and sensor readings to monitor an area inside our house. Since moving to a more rural property I have wanted to do a similar project but outside and solar powered - Raspberry Pi shortages, never quite finding an in-stock dedicated Pi solar setup that I loved and other house projects delayed that idea...

Recently we installed a 12V/200aH solar system near our parking area. The main purpose of this system is to power the rodent deterrent lights for our trucks - but it has more than enough power to also power a Pi for photo purposes!

### Equipment:
 - [Raspberry Pi 3 Model A+](https://www.raspberrypi.com/products/raspberry-pi-3-model-a-plus/) with [5V 2.5A Switching Power Supply with 20AWG MicroUSB Cable](https://www.adafruit.com/product/1995) and [a case from Adafruit](https://www.adafruit.com/product/2359): This is about $40 USD plus shipping - I like the $25 USD price, full sized HDMI port and slim profile compared to a full 3/4/5.
 - [Raspberry Pi Camera Module 3 - 12MP 120 Degree Wide Angle Lens](https://www.adafruit.com/product/5658): I love photography - you can see some of my work over on [Pointless Waymarks](https://pointlesswaymarks.com/) - so considered a number of choices for this project but in the end the cost/convenience/size/performance of going with a $35 official camera module won out.
 - Wood and Plexiglass Enclosure: Hopefully weatherproof (enough)! Built with spare/scrap wood, bug screen sitting in a closet, caulk from working on floor transitions and the paint for the deck. The main feature is that I recycled existing materials for this! As you can see in the photos the carpentry is very basic and no details are included. The challenge I found with making a simple enclosure was weatherproofing the camera exit from the enclosure:
  - I tried using plexiglass for the entire front panel of the enclosure - but at least with the plexiglass I had the images were never sharp, I was using plexiglass left over from another (not camera oriented) projected. Different plexiglass may work better but I didn't want to dive into figuring out 'best optical quality plexiglass' (and didn't want glass for durability reasons) so moved on.
  - To move to another strategy I mounted the camera on the front panel of the enclosure - here I made my next mistake and mounted the camera tightly to the front panel - this didn't work, if you mount the camera package tightly against something you can end up impacting focus and not being able to get good photographs... In the end both for focus and making sure the wide angle camera has a clear view I simple made a larger hole in the panel for the camera.
  - Next I tried a plexiglass dome off of Amazon - this was great for part of the photograph but distorted the edges. It's possible that the distortion would go away if I mounted the camera farther into the dome, but I wasn't interested in that complication..
  - And the solution that finally worked for me was using a camera lens UV filter and hot gluing it to the outside of the eclosure. I used an $8 [Tiffen 55mm UV Protector Filters](https://www.bhphotovideo.com/c/product/72714-REG/Tiffen_55UVP_55mm_UV_Protector.html) - it is easy to find smaller diameter filters but after some experiments with filters I already owned I liked this size because it was very easy to position it so that the edge of the filter didn't get into the photo.
 - Solar: As mentioned above powering the system with solar was a goal - but it turns out that the solar system powering my setup wasn't designed primarily to run the Pi - for the sake of documenting the project the main components of the system are listed below. This system is massive overkill if you just want to run a few Pis, like most real world systems I had many constraints and goals that are not in line with 'build the world's best small solar system' and this is not a recommendation regarding anything below, I don't have enough experience to do that, but just to document what I am actually doing my equipment list is below (wiring and fuses omitted - btw if you are building a system of this size or larger for the first time be sure to look up wiring and fuses/breaker cost - it was much more than I guessed...).
	- 3x [Newpowa 100W 12V Mono Compact Solar Panels](https://www.newpowa.com/new-100w-compact-12v-mono-solar-panel/)
	- 2x [Ampere Time 12V 100Ah Lithium Batteries](https://www.amperetime.com/collections/ampere-time-12v-lithium-lifepo4-battery-series/products/ampere-time-12v-100ah-lithium-lifepo4-battery) - purchased used.
	- [Victron Energy SmartSolar MPPT 100/20](https://www.victronenergy.com/solar-charge-controllers/smartsolar-mppt-75-10-75-15-100-15-100-20)
	- [Victron Phoenix 12V/800W Inverter](https://www.victronenergy.com/inverters/phoenix-inverter-vedirect-250va-800va) with [Victron VE.Direct Bluetooth Smart Dongle](https://www.victronenergy.com/accessories/ve-direct-bluetooth-smart-dongle)
	- [Victron SmartShunt](https://www.victronenergy.com/battery-monitors/smart-battery-shunt)
	- [Raspberry Pi 3 Model A+](https://www.raspberrypi.com/products/raspberry-pi-3-model-a-plus/) running the [Victron Energy Venus OS](https://github.com/victronenergy/venus) to provide communication between the system and the [Victron Remote Monitoring System](https://www.victronenergy.com/panel-systems-remote-monitoring/vrm). See [Panbo's Raspberry Pi Victron Venus OS Install post](https://panbo.com/victrons-venus-os-on-a-raspberry-pi-install-and-configuration/) and as of 9/18/2023 see [Raspberry Pi 3A+: VRM Portal ID Missing](https://community.victronenergy.com/questions/79169/raspberry-pi-3a-vrm-id-missing.html) for critical information on getting Venus OS working correctly on the 3 A+. I used 2 [VE.Direct to USB interface cables](https://www.victronenergy.com/accessories/ve-direct-to-usb-interface) to connect the SmartSolar and SmartShunt to the Pi (currently the Bluetooth interface on the SmartSolar and SmartShunt is NOT used to connect to Victron Venus OS/Cerbo GX units! The Bluetooth does create a pretty great app experience, at least on Android...).

### Setup Notes

If you've worked in years gone by with the Pi Camera and C# you might have found the very useful [techyian/MMALSharp: C# wrapper to Broadcom's MMAL with an API to the Raspberry Pi camera.](https://github.com/techyian/MMALSharp) - without choosing an older version of Raspberry Pi OS that library doesn't work - the Pi has moved on to [libcamera](https://libcamera.org/). I didn't find a C# wrapper for libcamera and since I didn't need to do anything other than write stills to the Pi's storage simply calling libcamera-still 'command line style' seemed to be the best option.

I didn't find a single great place for libcamera-still documentation - frustrating until I figured out that (beyond 'getting started' content) running 'libcamera-still --help' was really the best starting spot.

[How To Easily Disable Status LEDs On Raspberry Pi � RaspberryTips](https://raspberrytips.com/disable-leds-on-raspberry-pi/)

## Other Projects

Fundamentally this project is just taking photographs with the Raspberry Pi which is not hard to do and there are free projects that do more than just take stills! One of my favorites is [GitHub - thomasjacquin's allsky: A Raspberry Pi operated Wireless Allsky Camera](https://github.com/thomasjacquin/allsky) - I hope to build on of these in the future...

## Tools and Libraries

This program would not be possible without the amazing resources available for creating Free software! Used in this project:

**Tools:**
  - [Visual Studio IDE](https://visualstudio.microsoft.com/), [.NET Core (Linux, macOS, and Windows)](https://dotnet.microsoft.com/download/dotnet-core)
  - [ReSharper: The Visual Studio Extension for .NET Developers by JetBrains](https://www.jetbrains.com/resharper/)
  - [GitHub Copilot � Your AI pair programmer � GitHub](https://github.com/features/copilot)
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
  - [Codeuctivity/SkiaSharp.Compare: Adds compare features on top of SkiaSharp](https://github.com/Codeuctivity/SkiaSharp.Compare). Apache-2.0 License.


