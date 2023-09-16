## Pi Sliced Day Photos

This is a small program designed to be run on a Raspberry Pi that takes photographs at Sunrise, Sunset and a number of specified intervals in-between.

By anchoring the pictures to Sunrise, Sunset and a number of divided intervals in-between that rather than having the same clock time for each set of photos you instead have the same relative time during the day or night. Probably the best example of 'why' is wanting a photo at sunrise, half way thru the day light and at sunset.

For this program to work it requires:
 - A settings file named PiSlicedDaySettings.json
 - A CSV file named SunriseAndSunset.csv with the calendar day, sunrise time (local) and sunset time (local)

In the settings file you can specify how many photographs you want taken between Sunrise and Sunset (during the day) and between Sunset and Sunrise (during the night) - zero is a valid setting. Sunrise and Sunset are always photographed.

The reason this program uses a file of Sunrise/Sunset times is to allow you to input any sunrise/sunset times that you want - a strong use case for this is generating topography compensated Sunrise/Sunset times for your location. For example at my home we didn't see the sun come over the mountains to the east until 40+ minutes after the sunrise time that my devices were reporting...

If you are interested in generated Topography compensated Sunrise/Sunset times for your location try:
  - [gvellut/tppss: Compute sunrise / sunset times taking into account local topography](https://github.com/gvellut/tppss) - this is a great free way to generate the times - it does take some setup, but like me you might find preparing the data for the program is an interesting learning project!
  - [Find Your Location and Compute Sunlight Conditions](https://www.suncurves.com/en/) - a paid service that will do this for you.
  - Various photography apps can calculate/show you this information but I'm not sure if any of them export yearly (or multi-year) data...