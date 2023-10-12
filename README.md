## Pi Sliced Day Photos

This is a small .NET Core (C#) program designed to be run on a Raspberry Pi that takes photographs at Sunrise, Sunset, a number of specified intervals in-between and at custom times specified either by clock time or minutes +/- from sunrise/sunset.

The in-between intervals are calculated based on sunrise/sunset times and will not necessarily be at the same clock time each day - instead they will be at the same  relative time thru the day or night.

For me, living in the shadows of mountain peaks, the amount of daylight/darkness at my house has become a frame for my life. A system that takes a photograph 'half way thru the light for the day' is more interesting than a photo taken at a certain clock time or sun angle.

For this program to work it requires:
 - A Raspberry Pi where .NET Core can run and an attached camera that responds to libcamera-still. This program has only been confirmed to run on a Raspberry Pi 3 A+ with a Camera Module 3 Wide - but I believe any 2+ version Pis that can run current versions of the Raspberry Pi OS should work paired with any of the official Pi camera modules... 
 - A settings file named PiSlicedDaySettings.json - an example is included in the code
 - A CSV file named SunriseAndSunset.csv with the calendar day, sunrise time (local) and sunset time (local)

Sunrise and Sunset are always photographed.

In the settings file you can specify how many photographs you want taken between Sunrise and Sunset (during the day - 0 is valid), between Sunset and Sunrise (during the night - 0 is valid).

You can also specify custom times in the settings file either as clock times or as Sunrise/Sunset +/- minutes.


### SunriseAndSunset.csv

This program uses a file of Sunrise/Sunset times to allow you to input any sunrise/sunset times that you want - a strong use case for this is generating topography compensated Sunrise/Sunset times for your location. For example today at my home we didn't see the sun come over the mountains to the east until 40+ minutes after the sun rise time calculated without the topography of the mountains...

The SunriseAndSunset.csv file should be formated like the the sample file included with the program:
```
DAY, SUNRISE, SUNSET
2023-01-01,08:15:00-0700,17:23:00-0700
2023-01-02,08:15:00-0700,17:24:00-0700
```

If you are interested in generating Topography compensated Sunrise/Sunset times for your location try:
  - [gvellut/tppss: Compute sunrise / sunset times taking into account local topography](https://github.com/gvellut/tppss) - this is a great free way to generate the times - it does take some setup, but like me you might find preparing the data for the program is an interesting learning project!
  - [Find Your Location and Compute Sunlight Conditions](https://www.suncurves.com/en/) - a paid service that will do this for you.
  - Various photography apps can calculate/show you this information but I'm not sure if any of them export yearly (or multi-year) data...


### Settings File

_DaySlices_ - Takes an integer and determines the number of photos taken between sunrise and sunset

_NightSlices_ - Takes an integer and determines the number of photos taken between sunset and sunrise

_PhotoStorageDirectory_ - the path to where the photos are written

_SunriseSunsetCsvFile_ - the name (including extension) of the csv file of Sunrise and Sunset times

_PhotoNamePrefix_ - Prefix for the photo names, the date and time will follow the prefix and .jpg to create the filename. It is especially useful to change this if you have multiple cameras running

_LogFullExceptionsToImages_ - The assumption is that this program will run largely unattended in the background and that most of the time the only thing you will see from the program is the photographs. The program will try to alert you of errors by writing exception information to an error file in the PhotoStorageDirectory. This setting determines whether the program writes all of the exception information or only an abbreviated message. This option exists because writing full exception information may leak information about your setup!

_LibCameraParameters_ - Day/Night/Sunset/Sunrise - command line parameters for libcamera-still

_CustomTimes_ - custom times can be specified either as clock times (3:45 pm) or as minutes before/after sunset (sunset +10). Each custom time also gets LibCamera parameters.

```
"CustomTimes": [
    {
      "Time": "Sunset+10",
      "LibCameraParameters": "--autofocus-mode manual --lens-position 0 --awb auto --metering average --denoise cdn_hq"
    },
    {
      "Time": "Sunrise-10",
      "LibCameraParameters": "--autofocus-mode manual --lens-position 0 --awb auto --metering average --denoise cdn_hq"
    }
  ]
```


### Setup Notes

Suggested setup: Clone, build and publish this project to a folder - then on the Pi:
 - In your home directory - make a directory for the program and the photos:
	```
	mkdir PiSlicedDayPhotos
	mkdir SlicedPhotos
	```
	- Copy the published output of this solution into the PiSlicedDayPhotos folder - then change the permissions for the program to be executable:
	```
	chmod +x PiSlicedDayPhotos
	```
 - Run the program as a service: Edit the pisliceddayphotos.service and replace [Your Directory Here], copy it to /etc/systemd/system/, start and follow the service to check for any errors:
	```
	nano pisliceddayphotos.service
	sudo cp pisliceddayphotos.service /etc/systemd/system/
	sudo systemctl daemon-reload
	sudo systemctl start pisliceddayphotos
 	sudo systemctl enable pisliceddayphotos
	journalctl -u pisliceddayphotos -f
	```

I like to disable the LEDs to make sure that the glass covering the lens opening won't pick up any light from the LEDS - [How To Easily Disable Status LEDs On RaspberryTips](https://raspberrytips.com/disable-leds-on-raspberry-pi/)

My preference is for Automatic/Unattended Upgrades - do this long enough and something unexpected will break, but would rather stay up to date and have something break sooner rather than later. [Secure your Raspberry Pi by enabling automatic software updates – Sean Carney](https://www.seancarney.ca/2021/02/06/secure-your-raspberry-pi-by-enabling-automatic-software-updates/) and [UnattendedUpgrades - Debian Wiki](https://wiki.debian.org/UnattendedUpgrades)

If you've worked in years gone by with the Pi Camera and C# you might have found the very useful [techyian/MMALSharp: C# wrapper to Broadcom's MMAL with an API to the Raspberry Pi camera.](https://github.com/techyian/MMALSharp) - without choosing an older version of Raspberry Pi OS that library no longer works - the Pi has moved on to [libcamera](https://libcamera.org/). I didn't find a C# wrapper for libcamera and since I didn't need to do anything other than write stills to the Pi's storage simply calling libcamera-still 'command line style' seemed to be the best option.

I didn't find a single great place for libcamera-still documentation - frustrating until I figured out that (beyond 'getting started' content) running 'libcamera-still --help' was really the best single source of information.


### Backstory

For a number of years my wife and I used a previous (now-archived) project - [cmiles/PiDropLapse](https://github.com/cmiles/PiDropLapse/tree/main) - and a [Raspberry Pi 4 Model B](https://www.raspberrypi.com/products/raspberry-pi-4-model-b/) to take periodic photographs and sensor readings to monitor an area inside our house. Since moving to a more rural property I have wanted to do a similar project but outside and solar powered - Raspberry Pi shortages, never quite finding an in-stock dedicated Pi solar setup that I loved and other house projects delayed that idea...

Recently we installed a 12V/200aH solar system near our parking area. The main purpose of this system is to power the rodent deterrent lights for our trucks - but it has more than enough power to also power several Pis for photo purposes!


### My Setup with Notes
 - [Raspberry Pi 3 Model A+](https://www.raspberrypi.com/products/raspberry-pi-3-model-a-plus/), [5V 2.5A Switching Power Supply with 20AWG MicroUSB Cable](https://www.adafruit.com/product/1995), 32 GB MicroSD Card and [a case from Adafruit](https://www.adafruit.com/product/2359): This is about $60 USD plus shipping - I like the $25 USD price of the 3 A+, the full sized HDMI port and the slim profile.
 - [Raspberry Pi Camera Module 3 - 12MP 120 Degree Wide Angle Lens](https://www.adafruit.com/product/5658): I love photography - you can see some of my work over on [Pointless Waymarks](https://pointlesswaymarks.com/) - so considered a number of choices for this project but in the end the cost/convenience/size/performance of going with a $35 official camera module won out.
 - Wood Enclosure: Hopefully weatherproof (enough)! Built with spare/scrap wood, bug screen sitting in a closet and the paint for our deck. The main feature is that I recycled existing materials for this! As you can see in the photos the carpentry is very (very!) basic so no details are included. The challenge I found with making a simple enclosure was weatherproofing the hole for the camera lens:
  - I tried using plexiglass for the entire front panel of the enclosure - but at least with the plexiglass I had the images were never sharp. I was using plexiglass left over from another (not camera oriented) project and I didn't want to dive into figuring out 'best optical quality plexiglass' (and didn't want glass for durability reasons) so I moved on.
  - To move to another strategy I mounted the camera on the front panel of the enclosure - at first with the mistake of mounting the camera tightly to the front panel - this didn't work, if you mount the camera package tightly against something you can end up impacting focus... In the end both for focus and making sure the wide angle camera has a clear view I simple made a larger hole in the panel for the camera but that made it more important to find something to cover the hole for weatherproofing.
  - I tried a plexiglass dome off of Amazon to cover the exit hole for the camera - this was great for part of the photograph but distorted the edges. It's possible that the distortion would go away if I mounted the camera farther into the dome, but that added a complication to the design I wasn't interested in.
  - The solution that finally worked for me was using a UV lens filter and hot gluing it to the outside of the enclosure. I used an $8 [Tiffen 55mm UV Protector Filter](https://www.bhphotovideo.com/c/product/72714-REG/Tiffen_55UVP_55mm_UV_Protector.html) - it is easy to find smaller diameter filters but after some experiments I liked this size because it was very easy to position it so that the edge of the filter didn't end up in the photo.
 - With the mostly recycled enclosure the cost of the system is around $105 potentially a bit more with tax and shipping.
 - Solar: As mentioned above powering the system with solar was a goal - but it turns out that the solar system powering my setup wasn't designed primarily to run the Pi - for the sake of documenting the project the main components of the system are listed below. This system is massive overkill if you just want to run a few Pis, like most real world systems I had many constraints and goals that are not in line with 'build the world's best small solar system' and this is not a recommendation regarding anything below, I don't have enough experience to do that, but just to document what I am actually doing my equipment list is below (wiring and fuses omitted - btw if you are building a system of this size or larger for the first time be sure to look up wiring and fuses/breaker cost - it was much more than I guessed...).
	- 3x [Newpowa 100W 12V Mono Compact Solar Panels](https://www.newpowa.com/new-100w-compact-12v-mono-solar-panel/)
	- 2x [Ampere Time 12V 100Ah Lithium Batteries](https://www.amperetime.com/collections/ampere-time-12v-lithium-lifepo4-battery-series/products/ampere-time-12v-100ah-lithium-lifepo4-battery) - purchased used.
	- [Victron Energy SmartSolar MPPT 100/20](https://www.victronenergy.com/solar-charge-controllers/smartsolar-mppt-75-10-75-15-100-15-100-20)
	- [Victron Phoenix 12V/800W Inverter](https://www.victronenergy.com/inverters/phoenix-inverter-vedirect-250va-800va) with [Victron VE.Direct Bluetooth Smart Dongle](https://www.victronenergy.com/accessories/ve-direct-bluetooth-smart-dongle)
	- [Victron SmartShunt](https://www.victronenergy.com/battery-monitors/smart-battery-shunt)
	- [Raspberry Pi 3 Model A+](https://www.raspberrypi.com/products/raspberry-pi-3-model-a-plus/) running the [Victron Energy Venus OS](https://github.com/victronenergy/venus) to provide communication between the system and the [Victron Remote Monitoring System](https://www.victronenergy.com/panel-systems-remote-monitoring/vrm). See [Panbo's Raspberry Pi Victron Venus OS Install post](https://panbo.com/victrons-venus-os-on-a-raspberry-pi-install-and-configuration/) and as of 9/18/2023 see [Raspberry Pi 3A+: VRM Portal ID Missing](https://community.victronenergy.com/questions/79169/raspberry-pi-3a-vrm-id-missing.html) for critical information on getting Venus OS working correctly on the 3 A+. I used 2 [VE.Direct to USB interface cables](https://www.victronenergy.com/accessories/ve-direct-to-usb-interface) to connect the SmartSolar and SmartShunt to the Pi (currently the Bluetooth interface on the SmartSolar and SmartShunt is NOT used to connect to Victron Venus OS/Cerbo GX units! The Bluetooth does create a pretty great app experience, at least on Android...).


### Other Projects

Fundamentally this project is just taking photographs with the Raspberry Pi which is not hard to do and you can find other great free projects and code to take stills, timelapses and more! One of my favorites is [GitHub - thomasjacquin's allsky: A Raspberry Pi operated Wireless Allsky Camera](https://github.com/thomasjacquin/allsky) - I hope to build on of these in the future...


### Tools and Libraries

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
  - [Codeuctivity/SkiaSharp.Compare: Adds compare features on top of SkiaSharp](https://github.com/Codeuctivity/SkiaSharp.Compare). Apache-2.0 License.


